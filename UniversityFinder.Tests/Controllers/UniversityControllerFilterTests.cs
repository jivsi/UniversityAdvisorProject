using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UniversityFinder.Controllers;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Tests.Controllers;

public class UniversityControllerFilterTests
{
    [Fact]
    public async Task Index_WithSubjectFilter_IntersectsResults()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains("Subjects.name=eq.", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{uniId}}","Name":"TU","City":"Sofia","Country":"BG"}]""");
            if (uri.Contains("universities", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""
                [
                  {"Id":"{{uniId}}","Name":"TU","City":"Sofia","Country":"BG"},
                  {"Id":"22222222-2222-2222-2222-222222222222","Name":"Other","City":"Plovdiv","Country":"BG"}
                ]
                """);
            if (uri.Contains("UniversityPrograms", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (uri.Contains("cities", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("""[{"Id":1,"Name":"Sofia"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var controller = new UniversityController(
            Mock.Of<IUserFavoriteService>(),
            Mock.Of<IUserSearchHistoryService>(),
            Mock.Of<IUserUniversityHistoryService>(),
            supabase,
            NullLogger<UniversityController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Index(search: null, city: null, subject: "Math");

        var vm = Assert.IsType<UniversityIndexViewModel>(((ViewResult)result).Model!);
        Assert.Single(vm.Universities);
    }
}
