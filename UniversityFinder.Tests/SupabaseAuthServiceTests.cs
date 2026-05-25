using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Supabase.Gotrue;
using UniversityFinder.Services;

namespace UniversityFinder.Tests;

public class SupabaseAuthServiceTests
{
    [Fact]
    public void CreateClaimsPrincipalAsync_IncludesCoreClaims()
    {
        var service = new SupabaseAuthService(
            TestHelpers.TestServiceFactory.CreateConfiguration(),
            NullLogger<SupabaseAuthService>.Instance);

        var session = new Session
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            User = new User
            {
                Id = "user-123",
                Email = "test@example.com",
                UserMetadata = new Dictionary<string, object> { ["role"] = "Admin" }
            }
        };

        var principal = service.CreateClaimsPrincipalAsync(session);

        Assert.Equal("user-123", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("test@example.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal("Admin", principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("access", principal.FindFirst("access_token")?.Value);
    }

    [Fact]
    public void CreateClaimsPrincipalAsync_NullSession_Throws()
    {
        var service = new SupabaseAuthService(
            TestHelpers.TestServiceFactory.CreateConfiguration(),
            NullLogger<SupabaseAuthService>.Instance);

        Assert.Throws<ArgumentException>(() => service.CreateClaimsPrincipalAsync(null!));
    }

    [Fact]
    public void GetClient_ReturnsSupabaseClient()
    {
        var service = new SupabaseAuthService(
            TestHelpers.TestServiceFactory.CreateConfiguration(),
            NullLogger<SupabaseAuthService>.Instance);

        Assert.NotNull(service.GetClient());
    }
}
