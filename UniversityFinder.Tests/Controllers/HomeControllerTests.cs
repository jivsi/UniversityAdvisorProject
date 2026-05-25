using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using UniversityFinder.Controllers;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public async Task Index_LoadsStatisticsAndCities()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("""
            [
              {"Id":"11111111-1111-1111-1111-111111111111","Name":"TU","City":"Sofia","Country":"BG"},
              {"Id":"22222222-2222-2222-2222-222222222222","Name":"UNSS","City":"Sofia","Country":"BG"}
            ]
            """));

        var controller = new HomeController(NullLogger<HomeController>.Instance, supabase);

        var result = await controller.Index();

        Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewBag.Cities);
        Assert.Equal(2, controller.ViewBag.UniversityCount);
        Assert.Equal(1, controller.ViewBag.RegionCount);
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance, TestServiceFactory.CreateSupabaseService());
        Assert.IsType<ViewResult>(controller.Privacy());
    }

    [Fact]
    public void Error_ReturnsViewWithRequestId()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance, TestServiceFactory.CreateSupabaseService())
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };

        var result = controller.Error();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UniversityFinder.Models.ErrorViewModel>(view.Model);
        Assert.False(string.IsNullOrEmpty(model.RequestId));
    }
}
