using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UniversityFinder.Controllers;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.ViewModels;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests.Controllers;

public class UniversityControllerExtendedTests
{
    [Fact]
    public async Task Index_NoSearch_AppliesCityFilter()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""
            [{"Id":"{{uniId}}","Name":"TU","City":"Sofia","Country":"BG"}]
            """));

        var controller = new UniversityController(
            Mock.Of<IUserFavoriteService>(),
            Mock.Of<IUserSearchHistoryService>(),
            Mock.Of<IUserUniversityHistoryService>(),
            supabase,
            NullLogger<UniversityController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Index(search: null, city: "Sofia", subject: null);

        var vm = Assert.IsType<UniversityIndexViewModel>(((ViewResult)result).Model!);
        Assert.Single(vm.Universities);
    }

    [Fact]
    public async Task Details_ReturnsUniversity_WhenFound()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""
            [{"Id":"{{uniId}}","Name":"TU Sofia","City":"Sofia","Country":"BG"}]
            """));

        var controller = new UniversityController(
            Mock.Of<IUserFavoriteService>(),
            Mock.Of<IUserSearchHistoryService>(),
            Mock.Of<IUserUniversityHistoryService>(),
            supabase,
            NullLogger<UniversityController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Details("TU Sofia");

        var view = Assert.IsType<ViewResult>(result);
        var uni = Assert.IsType<University>(view.Model);
        Assert.Equal("TU Sofia", uni.Name);
    }

    [Fact]
    public async Task Autocomplete_ReturnsMatches()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("""
            [{"Id":"11111111-1111-1111-1111-111111111111","Name":"TU Sofia","City":"Sofia"}]
            """));

        var controller = new UniversityController(
            Mock.Of<IUserFavoriteService>(),
            Mock.Of<IUserSearchHistoryService>(),
            Mock.Of<IUserUniversityHistoryService>(),
            supabase,
            NullLogger<UniversityController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Autocomplete("sof");

        var json = Assert.IsType<JsonResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(json.Value);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task Favorites_WithUser_ReturnsView()
    {
        var favorites = new Mock<IUserFavoriteService>();
        favorites.Setup(s => s.GetUserFavoritesAsync("user-1"))
            .ReturnsAsync(new List<University> { new() { Name = "TU" } });

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-1")],
            "test"));

        var controller = new UniversityController(
            favorites.Object,
            Mock.Of<IUserSearchHistoryService>(),
            Mock.Of<IUserUniversityHistoryService>(),
            TestServiceFactory.CreateSupabaseService(),
            NullLogger<UniversityController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };

        var result = await controller.Favorites();
        Assert.IsType<ViewResult>(result);
    }
}
