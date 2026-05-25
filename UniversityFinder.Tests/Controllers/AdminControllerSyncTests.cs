using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UniversityFinder.Controllers;
using UniversityFinder.DTOs;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests.Controllers;

public class AdminControllerSyncTests
{
    [Fact]
    public async Task SyncFromNacid_WhenImportThrows_SetsErrorMessage()
    {
        var nacidUniversities = JsonSerializer.Serialize(new NacidUniversityResponse
        {
            Result = [new NacidUniversity { Id = 1, Name = "New Uni", IsActive = true }]
        });

        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (req.Method == HttpMethod.Get && uri.Contains("Name=ilike", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (req.Method == HttpMethod.Post && uri.Contains("/rest/v1/universities", StringComparison.OrdinalIgnoreCase))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var nacid = TestServiceFactory.CreateNacidScraperService(supabase, req =>
        {
            if (req.RequestUri!.AbsoluteUri.Contains("Universities", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(nacidUniversities);
            return FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}""");
        });

        var controller = new AdminController(supabase, nacid, NullLogger<AdminController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        var result = await controller.SyncFromNacid();

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["ErrorMessage"]);
    }
}
