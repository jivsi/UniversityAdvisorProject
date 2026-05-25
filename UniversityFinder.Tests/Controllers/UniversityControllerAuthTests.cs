using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UniversityFinder.Controllers;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests.Controllers;

public class UniversityControllerAuthTests
{
    private static ClaimsPrincipal AuthenticatedUser(string id = "user-1") =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, id)],
            "test"));

    [Fact]
    public async Task Details_AuthenticatedUser_TracksVisitAndFavoriteState()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains("Name=ilike", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(
                    "[{\"Id\":\"" + uniId + "\",\"Name\":\"TU Sofia\",\"City\":\"Sofia\",\"Country\":\"BG\"}]");
            if (uri.Contains("UserFavorites", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(
                    "[{\"Id\":\"22222222-2222-2222-2222-222222222222\",\"UserId\":\"user-1\",\"UniversityId\":\"" + uniId + "\"}]");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var history = new Mock<IUserUniversityHistoryService>();
        history.Setup(s => s.TrackVisitAsync("user-1", uniId)).Returns(Task.CompletedTask);

        var controller = new UniversityController(
            Mock.Of<IUserFavoriteService>(),
            Mock.Of<IUserSearchHistoryService>(),
            history.Object,
            supabase,
            NullLogger<UniversityController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = AuthenticatedUser() }
            }
        };

        var result = await controller.Details("TU Sofia");

        var view = Assert.IsType<ViewResult>(result);
        Assert.True((bool)controller.ViewBag.IsFavorited!);
        history.Verify(s => s.TrackVisitAsync("user-1", uniId), Times.Once);
        Assert.IsType<University>(view.Model);
    }

    [Fact]
    public async Task ToggleFavoriteByGuid_ValidRequest_ReturnsJson()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.Method == HttpMethod.Get)
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (req.Method == HttpMethod.Post)
                return FakeHttpMessageHandler.JsonResponse("""[{"Id":"1"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = new UniversityController(
            Mock.Of<IUserFavoriteService>(),
            Mock.Of<IUserSearchHistoryService>(),
            Mock.Of<IUserUniversityHistoryService>(),
            supabase,
            NullLogger<UniversityController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = AuthenticatedUser() }
            }
        };

        var result = await controller.ToggleFavoriteByGuid(uniId.ToString());

        Assert.IsType<JsonResult>(result);
    }
}
