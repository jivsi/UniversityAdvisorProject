using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniversityAdvisor.Domain.Entities;
using UniversityAdvisor.ViewModels;
using UniversityAdvisor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace UniversityAdvisor.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Country = model.Country
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError("", "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile(string? userId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var id = userId ?? currentUserId;
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && id != currentUserId)
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var ratings = await _db.Ratings
            .Where(r => r.UserId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ViewModels.UserRatingProfileEntry
            {
                UniversityName = r.University != null
                    ? r.University.Name
                    : _db.Universities
                        .Where(u => u.Id == r.UniversityId)
                        .Select(u => u.Name)
                        .FirstOrDefault(),
                UniversityId = r.UniversityId,
                Score = r.Score,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        var model = new ViewModels.UserRatingProfileViewModel
        {
            User = user,
            Ratings = ratings
        };

        return View("Profile", model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl!);
        }
        return RedirectToAction("Index", "Home");
    }
}
