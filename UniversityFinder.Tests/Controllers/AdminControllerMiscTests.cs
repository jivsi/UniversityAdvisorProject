using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UniversityFinder.Controllers;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests.Controllers;

public class AdminControllerMiscTests
{
    private static AdminController CreateController(SupabaseService supabase)
    {
        var controller = new AdminController(
            supabase,
            TestServiceFactory.CreateNacidScraperService(supabase),
            NullLogger<AdminController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task DebugSupabase_OnHttpError_ReturnsErrorContent()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var controller = CreateController(supabase);

        var result = await controller.DebugSupabase();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Contains("401", content.Content);
    }

    [Fact]
    public async Task Edit_Post_WhenUpdateReturnsNull_ReturnsViewWithError()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (req.Method == HttpMethod.Patch && uri.Contains($"Id=eq.{id}", StringComparison.OrdinalIgnoreCase))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            if (req.Method == HttpMethod.Get && uri.Contains($"Id=eq.{id}", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{id}}","Name":"Old","City":"Sofia","Country":"BG"}]""");
            if (uri.Contains("Name=ilike", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = CreateController(supabase);
        var result = await controller.Edit(id, new University { Name = "New", Country = "BG", City = "Sofia" });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Edit_Post_WhenNameAlreadyUsed_ReturnsViewWithError()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var otherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains($"Id=eq.{id}", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{id}}","Name":"Old","City":"Sofia","Country":"BG"}]""");
            if (uri.Contains("Name=ilike", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{otherId}}","Name":"Taken","City":"Sofia","Country":"BG"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = CreateController(supabase);
        var result = await controller.Edit(id, new University { Name = "Taken", Country = "BG", City = "Sofia" });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }
}
