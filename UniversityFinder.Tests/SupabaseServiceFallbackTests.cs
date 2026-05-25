using System.Net;
using UniversityFinder.Tests.TestHelpers;
using UniversityFinder.Services;

namespace UniversityFinder.Tests;

public class SupabaseServiceFallbackTests
{
    [Fact]
    public async Task GetUserFavoritesAsync_FallsBackWhenJoinFails()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains("UserFavorites", StringComparison.OrdinalIgnoreCase) &&
                uri.Contains("universities", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]", HttpStatusCode.BadRequest);

            if (uri.Contains("UserFavorites", StringComparison.OrdinalIgnoreCase) &&
                uri.Contains("select=*", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(
                    "[{\"Id\":\"22222222-2222-2222-2222-222222222222\",\"UserId\":\"u1\",\"UniversityId\":\"" + uniId + "\"}]");

            if (uri.Contains($"Id=eq.{uniId}", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(
                    "[{\"Id\":\"" + uniId + "\",\"Name\":\"TU\",\"City\":\"Sofia\",\"Country\":\"BG\"}]");

            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var favorites = await service.GetUserFavoritesAsync("u1");

        Assert.Single(favorites);
        Assert.Equal("TU", favorites[0].Name);
    }
}
