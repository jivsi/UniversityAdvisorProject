using Microsoft.Extensions.Logging.Abstractions;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Tests;

public class UserServicesTests
{
    [Fact]
    public async Task UserFavoriteService_GetUserFavorites_ReturnsResults()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var json = "[{\"universities\":{\"Id\":\"" + uniId + "\",\"Name\":\"TU\",\"City\":\"Sofia\",\"Country\":\"BG\"}}]";
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        var service = new UserFavoriteService(supabase, NullLogger<UserFavoriteService>.Instance);

        var favorites = await service.GetUserFavoritesAsync("u1");
        Assert.Single(favorites);
    }

    [Fact]
    public async Task UserSearchHistoryService_TracksSearchWithoutThrowing()
    {
        var supabase = TestServiceFactory.CreateSupabaseService();
        var service = new UserSearchHistoryService(supabase, NullLogger<UserSearchHistoryService>.Instance);

        await service.TrackSearchAsync("u1", new SearchViewModel { Query = "math", TotalResults = 2 });
    }

    [Fact]
    public async Task UserUniversityHistoryService_TracksAndLoadsVisits()
    {
        var uniId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.Method == HttpMethod.Post)
                return FakeHttpMessageHandler.JsonResponse("[]");
            return FakeHttpMessageHandler.JsonResponse($$"""
            [
              {
                "universities": {
                  "Id": "{{uniId}}",
                  "Name": "TU",
                  "City": "Sofia",
                  "Country": "BG"
                }
              }
            ]
            """);
        });

        var service = new UserUniversityHistoryService(supabase, NullLogger<UserUniversityHistoryService>.Instance);

        await service.TrackVisitAsync("u1", uniId);
        var visits = await service.GetRecentVisitsAsync("u1", 6);

        Assert.Single(visits);
    }
}
