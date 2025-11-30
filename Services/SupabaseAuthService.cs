using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Gotrue;
using System.Security.Claims;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for handling Supabase authentication
    /// Replaces ASP.NET Identity with Supabase Auth
    /// </summary>
    public class SupabaseAuthService
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly ILogger<SupabaseAuthService> _logger;

        public SupabaseAuthService(IConfiguration config, ILogger<SupabaseAuthService> logger)
        {
            _logger = logger;
            
            var url = config["Supabase:Url"]!;
            var anonKey = config["Supabase:AnonKey"]!;

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            _supabaseClient = new Supabase.Client(url, anonKey, options);
        }

        public async Task<(bool Success, string? ErrorMessage, Supabase.Gotrue.User? User)> RegisterAsync(string email, string password)
        {
            try
            {
                var response = await _supabaseClient.Auth.SignUp(email, password);
                
                if (response?.User != null)
                {
                    _logger.LogInformation("✅ User registered successfully: {Email}", email);
                    return (true, null, response.User);
                }
                
                return (false, "Registration failed. Please try again.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Registration error for {Email}: {Message}", email, ex.Message);
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? ErrorMessage, Supabase.Gotrue.Session? AuthSession)> LoginAsync(string email, string password)
        {
            try
            {
                var signInResponse = await _supabaseClient.Auth.SignIn(email, password);
                
                // Extract session from response - SignIn returns Session directly, not a response object
                Supabase.Gotrue.Session? session = signInResponse;
                
                if (session != null)
                {
                    _logger.LogInformation("✅ User logged in successfully: {Email}", email);
                    return (true, null, session);
                }
                
                return (false, "Invalid email or password.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Login error for {Email}: {Message}", email, ex.Message);
                return (false, ex.Message, null);
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _supabaseClient.Auth.SignOut();
                _logger.LogInformation("✅ User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Logout error: {Message}", ex.Message);
            }
        }

        public async Task<Supabase.Gotrue.User?> GetCurrentUserAsync()
        {
            try
            {
                var user = _supabaseClient.Auth.CurrentUser;
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting current user: {Message}", ex.Message);
                return null;
            }
        }

        public ClaimsPrincipal CreateClaimsPrincipalAsync(Supabase.Gotrue.Session session)
        {
            if (session?.User == null)
            {
                throw new ArgumentException("Session or User is null", nameof(session));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, session.User.Id ?? ""),
                new Claim(ClaimTypes.Email, session.User.Email ?? ""),
                new Claim(ClaimTypes.Name, session.User.Email ?? ""),
                new Claim("access_token", session.AccessToken ?? ""),
                new Claim("refresh_token", session.RefreshToken ?? "")
            };

            // Add role claim from user metadata if present
            if (session.User.UserMetadata != null)
            {
                // Check for role in user metadata
                if (session.User.UserMetadata.TryGetValue("role", out var roleValue) && roleValue != null)
                {
                    var role = roleValue.ToString();
                    if (!string.IsNullOrEmpty(role))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                        _logger.LogInformation("✅ Added role claim: {Role} for user {Email}", role, session.User.Email);
                    }
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(claimsIdentity);
        }

        public Supabase.Client GetClient() => _supabaseClient;
    }
}

