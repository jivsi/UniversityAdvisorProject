using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UniversityFinder.Controllers;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests.Controllers;

public class AccountControllerTests
{
    private static AccountController CreateController(SupabaseAuthService auth)
    {
        var controller = new AccountController(auth, NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static SupabaseAuthService CreateAuthService() =>
        new(TestServiceFactory.CreateConfiguration(), NullLogger<SupabaseAuthService>.Instance);

    [Fact]
    public void Login_Get_ReturnsView()
    {
        var controller = CreateController(CreateAuthService());
        Assert.IsType<ViewResult>(controller.Login());
    }

    [Fact]
    public async Task Login_Post_EmptyCredentials_AddsModelError()
    {
        var controller = CreateController(CreateAuthService());
        var result = await controller.Login("", "", null);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Register_Post_PasswordMismatch_AddsModelError()
    {
        var controller = CreateController(CreateAuthService());
        var result = await controller.Register("a@b.com", "secret1", "secret2", null);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Register_Post_ShortPassword_AddsModelError()
    {
        var controller = CreateController(CreateAuthService());
        var result = await controller.Register("a@b.com", "123", "123", null);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public void AccessDenied_ReturnsView()
    {
        Assert.IsType<ViewResult>(CreateController(CreateAuthService()).AccessDenied());
    }
}
