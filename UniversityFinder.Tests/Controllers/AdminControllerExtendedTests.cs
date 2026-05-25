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

public class AdminControllerExtendedTests
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
    public async Task Create_Post_ValidUniversity_RedirectsToIndex()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains("Name=ilike", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (req.Method == HttpMethod.Post && uri.Contains("universities", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("""
                [{"Id":"11111111-1111-1111-1111-111111111111","Name":"Brand New","City":"Sofia","Country":"BG"}]
                """);
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = CreateController(supabase);
        var result = await controller.Create(new University { Name = "Brand New", Country = "BG", City = "Sofia" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Index), redirect.ActionName);
    }

    [Fact]
    public async Task Edit_Get_NotFound_Redirects()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var controller = CreateController(supabase);
        var id = Guid.NewGuid();

        var result = await controller.Edit(id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Index), redirect.ActionName);
    }

    [Fact]
    public async Task Edit_Post_UpdatesUniversity()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains($"Id=eq.{id}", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{id}}","Name":"Old","City":"Sofia","Country":"BG"}]""");
            if (uri.Contains("Name=ilike", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (req.Method == HttpMethod.Patch)
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{id}}","Name":"New Name","City":"Sofia","Country":"BG"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = CreateController(supabase);
        var result = await controller.Edit(id, new University { Name = "New Name", Country = "BG", City = "Sofia" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Index), redirect.ActionName);
    }

    [Fact]
    public async Task DeleteConfirmed_DeletesUniversity()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.Method == HttpMethod.Delete)
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{id}}","Name":"TU","City":"Sofia","Country":"BG"}]""");
        });

        var controller = CreateController(supabase);
        var result = await controller.DeleteConfirmed(id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Index), redirect.ActionName);
    }

    [Fact]
    public async Task ManagePrograms_ReturnsView()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var subjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains($"Id=eq.{id}", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{id}}","Name":"TU","City":"Sofia","Country":"BG"}]""");
            if (uri.Contains("UniversityPrograms", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (uri.Contains("SubjectCategories", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (uri.Contains("Subjects", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{subjectId}}","Name":"Math"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = CreateController(supabase);
        var result = await controller.ManagePrograms(id);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task AddProgram_WithExistingSubject_Redirects()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var subjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains("Subjects", StringComparison.OrdinalIgnoreCase) && req.Method == HttpMethod.Get)
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{subjectId}}","Name":"Math"}]""");
            if (uri.Contains("UniversityPrograms", StringComparison.OrdinalIgnoreCase) && req.Method == HttpMethod.Post)
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"33333333-3333-3333-3333-333333333333"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = CreateController(supabase);
        var result = await controller.AddProgram(uniId, subjectId, null, null, "бакалавър", 4, "Редовно");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.ManagePrograms), redirect.ActionName);
    }

    [Fact]
    public async Task RemoveProgram_Redirects()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var programId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
            req.Method == HttpMethod.Delete
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : FakeHttpMessageHandler.JsonResponse("[]"));

        var controller = CreateController(supabase);
        var result = await controller.RemoveProgram(programId, uniId);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task DebugSupabase_ReturnsContent()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var controller = CreateController(supabase);

        var result = await controller.DebugSupabase();

        Assert.IsType<ContentResult>(result);
    }

    [Fact]
    public async Task Delete_Get_ReturnsView()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{id}}","Name":"TU","City":"Sofia","Country":"BG"}]"""));

        var controller = CreateController(supabase);
        var result = await controller.Delete(id);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task ReconcileSubjectCategoriesFromNacid_Redirects()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var nacid = TestServiceFactory.CreateNacidScraperService(supabase, _ =>
            FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}"""));

        var controller = CreateController(supabase, nacid);
        var result = await controller.ReconcileSubjectCategoriesFromNacid();

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task SyncFromNacid_RedirectsWithSuccess()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var nacid = TestServiceFactory.CreateNacidScraperService(supabase, _ =>
            FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}"""));

        var controller = CreateController(supabase, nacid);
        var result = await controller.SyncFromNacid();

        Assert.IsType<RedirectToActionResult>(result);
    }
}
