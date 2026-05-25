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

public class AdminControllerAddProgramTests
{
    [Fact]
    public async Task AddProgram_CreatesSubjectAndProgram_WhenManualNameProvided()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var subjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var categoryId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (req.Method == HttpMethod.Get && uri.Contains("/rest/v1/Subjects?", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (req.Method == HttpMethod.Post && uri.Contains("/rest/v1/Subjects", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(
                    "[{\"Id\":\"" + subjectId + "\",\"Name\":\"New Subject\"}]");
            if (req.Method == HttpMethod.Post && uri.Contains("UniversityPrograms", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(
                    "[{\"Id\":\"44444444-4444-4444-4444-444444444444\"}]");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = new AdminController(
            supabase,
            TestServiceFactory.CreateNacidScraperService(supabase),
            NullLogger<AdminController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        var result = await controller.AddProgram(
            uniId,
            subjectId: null,
            subjectName: "New Subject",
            categoryId: categoryId.ToString(),
            degreeType: "бакалавър",
            duration: 4,
            studyForm: "Редовно");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.ManagePrograms), redirect.ActionName);
    }
}
