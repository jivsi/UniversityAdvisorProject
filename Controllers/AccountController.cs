using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using UniversityFinder.Services;

namespace UniversityFinder.Controllers
{
    public class AccountController : Controller
    {
        private readonly SupabaseAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SupabaseAuthService authService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Email and password are required.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            var (success, errorMessage, authSession) = await _authService.LoginAsync(email, password);

            if (success && authSession != null)
            {
                // Create claims principal from Supabase session
                var principal = _authService.CreateClaimsPrincipalAsync(authSession);

                // Sign in using cookie authentication
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                _logger.LogInformation("✅ User logged in: {Email}", email);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", errorMessage ?? "Невалиден имейл или парола.");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Имейл и парола са задължителни.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Паролите не съвпадат.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Паролата трябва да бъде поне 6 символа.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            var (success, errorMessage, user) = await _authService.RegisterAsync(email, password);

            if (success && user != null)
            {
                _logger.LogInformation("✅ User registered: {Email}", email);
                TempData["SuccessMessage"] = "Регистрацията е успешна! Моля, влезте в системата.";
                return RedirectToAction("Login", new { returnUrl });
            }

            ModelState.AddModelError("", errorMessage ?? "Регистрацията е неуспешна. Моля, опитайте отново.");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("✅ User logged out");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

