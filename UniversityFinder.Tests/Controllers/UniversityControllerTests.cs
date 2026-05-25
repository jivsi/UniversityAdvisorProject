using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UniversityFinder.Controllers;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Tests.Controllers;

public class UniversityControllerTests
{
    private static UniversityController CreateController(
        SupabaseService supabase,
        Mock<IUserFavoriteService>? favorites = null,
        Mock<IUserSearchHistoryService>? searchHistory = null,
        Mock<IUserUniversityHistoryService>? visitHistory = null,
        ClaimsPrincipal? user = null)
    {
        favorites ??= new Mock<IUserFavoriteService>();
        searchHistory ??= new Mock<IUserSearchHistoryService>();
        visitHistory ??= new Mock<IUserUniversityHistoryService>();

        var controller = new UniversityController(
            favorites.Object,
            searchHistory.Object,
            visitHistory.Object,
            supabase,
            NullLogger<UniversityController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal() }
            }
        };

        return controller;
    }

    [Fact]
    public async Task Index_WithSearch_MergesNameAndSpecialtyResults()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var path = req.RequestUri!.AbsoluteUri;
            if (path.Contains("ProfessionalFields", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("Subjects.name=ilike", StringComparison.OrdinalIgnoreCase))
            {
                return FakeHttpMessageHandler.JsonResponse($$"""
                [{"Id":"{{uniId}}","Name":"TU","City":"Sofia","Country":"BG"}]
                """);
            }
            if (path.Contains("Name=ilike", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (path.Contains("UniversityPrograms", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (path.Contains("cities", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("""[{"Id":1,"Name":"Sofia"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = CreateController(supabase);
        var result = await controller.Index(search: "math", city: null, subject: null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<UniversityIndexViewModel>(view.Model);
        Assert.Single(vm.Universities);
    }

    [Fact]
    public async Task Details_NotFound_WhenUniversityMissing()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var controller = CreateController(supabase);

        var result = await controller.Details("Missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Autocomplete_ShortQuery_ReturnsEmptyJson()
    {
        var controller = CreateController(TestServiceFactory.CreateSupabaseService());
        var result = await controller.Autocomplete("a");

        var json = Assert.IsType<JsonResult>(result);
        Assert.Empty((System.Collections.IEnumerable)json.Value!);
    }

    [Fact]
    public async Task AutocompleteSubjects_ReturnsMatches()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("""
            [{"SubjectId":"11111111-1111-1111-1111-111111111111","Subject":{"Name":"Mathematics"}}]
            """));

        var controller = CreateController(supabase);
        var result = await controller.AutocompleteSubjects("mat");

        var json = Assert.IsType<JsonResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(json.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task ToggleFavorite_WithoutUser_ReturnsUnauthorized()
    {
        var controller = CreateController(TestServiceFactory.CreateSupabaseService());
        var result = await controller.ToggleFavorite(1);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ToggleFavoriteByGuid_InvalidId_ReturnsBadRequest()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-1")],
            "test"));

        var controller = CreateController(TestServiceFactory.CreateSupabaseService(), user: user);
        var result = await controller.ToggleFavoriteByGuid("not-a-guid");

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, status.StatusCode);
    }

    [Fact]
    public async Task Search_FiltersUniversitiesAndTracksHistory()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""
            [
              {
                "Id":"{{uniId}}",
                "Name":"TU Sofia",
                "City":"Sofia",
                "Country":"BG",
                "Programs":[{"DegreeType":"бакалавър"}]
              },
              {
                "Id":"22222222-2222-2222-2222-222222222222",
                "Name":"Other",
                "City":"Plovdiv",
                "Country":"BG",
                "Programs":[]
              }
            ]
            """));

        var searchHistory = new Mock<IUserSearchHistoryService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-1")],
            "test"));

        var controller = CreateController(supabase, searchHistory: searchHistory, user: user);
        var model = new SearchViewModel { Query = "TU", City = "Sofia", DegreeType = "бакалавър" };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(1, model.TotalResults);
        searchHistory.Verify(s => s.TrackSearchAsync("user-1", model), Times.Once);
        Assert.IsType<SearchViewModel>(view.Model);
    }
}
