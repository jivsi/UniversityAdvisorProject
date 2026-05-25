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

public class AdminControllerTests
{
    private static AdminController CreateController(SupabaseService supabase, NacidScraperService? nacid = null)
    {
        nacid ??= TestServiceFactory.CreateNacidScraperService(supabase);
        var controller = new AdminController(supabase, nacid, NullLogger<AdminController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task Sync_ReturnsView()
    {
        var controller = CreateController(TestServiceFactory.CreateSupabaseService());
        Assert.IsType<ViewResult>(await controller.Sync());
    }

    [Fact]
    public async Task SyncFromRvu_RedirectsWithError()
    {
        var controller = CreateController(TestServiceFactory.CreateSupabaseService());
        var result = await controller.SyncFromRvu();
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Sync), redirect.ActionName);
        Assert.NotNull(controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task GetSyncStatus_ReturnsIdleJson()
    {
        var controller = CreateController(TestServiceFactory.CreateSupabaseService());
        var result = await controller.GetSyncStatus();
        var json = Assert.IsType<JsonResult>(result);
        Assert.NotNull(json.Value);
    }

    [Fact]
    public async Task DebugUniversityCount_ReturnsCountJson()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("""
            [{"Id":"11111111-1111-1111-1111-111111111111","Name":"TU","City":"Sofia","Country":"BG"}]
            """));
        var controller = CreateController(supabase);

        var result = await controller.DebugUniversityCount();

        Assert.IsType<JsonResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsUniversitiesView()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var controller = CreateController(supabase);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<University>>(view.Model);
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView()
    {
        var controller = CreateController(TestServiceFactory.CreateSupabaseService());
        controller.ModelState.AddModelError("Name", "required");

        var result = await controller.Create(new University());

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Post_DuplicateName_ReturnsViewWithError()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("""
            [{"Id":"11111111-1111-1111-1111-111111111111","Name":"Existing","City":"Sofia","Country":"BG"}]
            """));
        var controller = CreateController(supabase);

        var result = await controller.Create(new University { Name = "Existing", Country = "BG", City = "Sofia" });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task DeleteConfirmed_NotFound_RedirectsWithError()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var controller = CreateController(supabase);
        var id = Guid.NewGuid();

        var result = await controller.DeleteConfirmed(id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Index), redirect.ActionName);
    }

    [Fact]
    public async Task SeedCategories_InsertsMissingCategories()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.RequestUri!.AbsoluteUri.Contains("SubjectCategories", StringComparison.OrdinalIgnoreCase) &&
                req.Method == HttpMethod.Get)
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (req.Method == HttpMethod.Post)
                return FakeHttpMessageHandler.JsonResponse("""[{"Id":"11111111-1111-1111-1111-111111111111","Name":"Tech"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = CreateController(supabase);
        var result = await controller.SeedCategories();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Sync), redirect.ActionName);
    }
}
