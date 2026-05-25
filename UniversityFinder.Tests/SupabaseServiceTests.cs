using System.Net;
using System.Text.Json;
using UniversityFinder.Models;
using UniversityFinder.Tests.TestHelpers;
using UniversityFinder.Services;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Tests;

public class SupabaseServiceTests
{
    private static readonly Guid SampleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetUniversitiesAsync_ReturnsDeserializedList()
    {
        var json = """[{"Id":"11111111-1111-1111-1111-111111111111","Name":"TU","City":"Sofia","Country":"Bulgaria"}]""";
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        var result = await service.GetUniversitiesAsync();

        Assert.Single(result);
        Assert.Equal("TU", result[0].Name);
    }

    [Fact]
    public async Task GetUniversitiesBySpecialtyAsync_EmptySearch_ReturnsEmpty()
    {
        var service = TestServiceFactory.CreateSupabaseService();
        var result = await service.GetUniversitiesBySpecialtyAsync("  ");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUniversitiesBySpecialtyAsync_OnFailure_ReturnsEmpty()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));
        var result = await service.GetUniversitiesBySpecialtyAsync("math");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUniversityByNameAsync_EmptyName_ReturnsNull()
    {
        var service = TestServiceFactory.CreateSupabaseService();
        Assert.Null(await service.GetUniversityByNameAsync(""));
    }

    [Fact]
    public async Task GetUniversityByNameAsync_ReturnsBestMatch()
    {
        var json = """[{"Id":"11111111-1111-1111-1111-111111111111","Name":"TU Sofia","City":"Sofia"}]""";
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        var result = await service.GetUniversityByNameAsync("TU Sofia");

        Assert.NotNull(result);
        Assert.Equal("TU Sofia", result!.Name);
    }

    [Fact]
    public async Task InsertUniversityAsync_OnSuccess_ReturnsUniversity()
    {
        var json = """[{"Id":"11111111-1111-1111-1111-111111111111","Name":"New"}]""";
        var service = TestServiceFactory.CreateSupabaseService(req =>
            req.Method == HttpMethod.Post
                ? FakeHttpMessageHandler.JsonResponse(json)
                : FakeHttpMessageHandler.JsonResponse("[]"));

        var inserted = await service.InsertUniversityAsync(new University { Name = "New", Country = "BG", City = "Sofia" });

        Assert.NotNull(inserted);
        Assert.Equal("New", inserted!.Name);
    }

    [Fact]
    public async Task InsertUniversityAsync_OnFailure_Throws()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.InsertUniversityAsync(new University { Name = "X", Country = "BG", City = "Sofia" }));
    }

    [Fact]
    public async Task DeleteUniversityAsync_ReturnsSuccessFlag()
    {
        var service = TestServiceFactory.CreateSupabaseService(req =>
            req.Method == HttpMethod.Delete
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : FakeHttpMessageHandler.JsonResponse("[]"));

        Assert.True(await service.DeleteUniversityAsync(SampleId));
    }

    [Fact]
    public async Task GetUniversityCountAsync_ReturnsCount()
    {
        var json = """[{"Id":"11111111-1111-1111-1111-111111111111"},{"Id":"22222222-2222-2222-2222-222222222222"}]""";
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        Assert.Equal(2, await service.GetUniversityCountAsync());
    }

    [Fact]
    public async Task GetProgramCountAsync_ReturnsArrayLength()
    {
        var json = """[{"Id":"1"},{"Id":"2"},{"Id":"3"}]""";
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        Assert.Equal(3, await service.GetProgramCountAsync());
    }

    [Fact]
    public async Task GetRegionCountAsync_CountsDistinctCities()
    {
        var json = """
        [
          {"Id":"11111111-1111-1111-1111-111111111111","Name":"A","City":"Sofia","Country":"BG"},
          {"Id":"22222222-2222-2222-2222-222222222222","Name":"B","City":"Plovdiv","Country":"BG"},
          {"Id":"33333333-3333-3333-3333-333333333333","Name":"C","City":"Sofia","Country":"BG"}
        ]
        """;
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        Assert.Equal(2, await service.GetRegionCountAsync());
    }

    [Fact]
    public async Task SyncUniversitiesAsync_SkipsEmptyNames()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));

        await service.SyncUniversitiesAsync(
        [
            new University { Name = " ", Country = "BG", City = "Sofia" },
            new University { Name = "Valid", Country = "BG", City = "Sofia" }
        ]);
    }

    [Fact]
    public async Task TrackSearchAsync_CompletesWithoutError()
    {
        var service = TestServiceFactory.CreateSupabaseService();
        await service.TrackSearchAsync("user-1", new SearchViewModel { Query = "test", TotalResults = 1 });
        await service.TrackSearchAsync("user-1", "q", 1, 2);
    }

    [Fact]
    public async Task IsFavoriteByGuidAsync_ReturnsTrueWhenRecordExists()
    {
        var json = $$"""
        [{"Id":"11111111-1111-1111-1111-111111111111","UserId":"u1","UniversityId":"{{SampleId}}"}]
        """;
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        Assert.True(await service.IsFavoriteByGuidAsync("u1", SampleId));
    }

    [Fact]
    public async Task IsFavoriteAsync_ReturnsFalseForEmptyArray()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));

        Assert.False(await service.IsFavoriteAsync("u1", 5));
    }

    [Fact]
    public async Task ToggleFavoriteAsync_AddsWhenMissing()
    {
        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.Method == HttpMethod.Get)
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (req.Method == HttpMethod.Post)
                return FakeHttpMessageHandler.JsonResponse("""[{"Id":1}]""");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        Assert.True(await service.ToggleFavoriteAsync("u1", 5));
    }

    [Fact]
    public async Task ToggleFavoriteByGuidAsync_RemovesWhenExists()
    {
        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsoluteUri.Contains("UserFavorites"))
                return FakeHttpMessageHandler.JsonResponse($$"""
                [{"Id":"11111111-1111-1111-1111-111111111111","UserId":"u1","UniversityId":"{{SampleId}}"}]
                """);
            if (req.Method == HttpMethod.Delete)
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        Assert.False(await service.ToggleFavoriteByGuidAsync("u1", SampleId));
    }

    [Fact]
    public async Task GetUserFavoritesAsync_ParsesNestedUniversities()
    {
        var json = $$"""
        [
          {
            "universities": {
              "Id": "{{SampleId}}",
              "Name": "TU",
              "City": "Sofia",
              "Country": "BG"
            }
          }
        ]
        """;
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        var favorites = await service.GetUserFavoritesAsync("u1");

        Assert.Single(favorites);
        Assert.Equal("TU", favorites[0].Name);
    }

    [Fact]
    public async Task GetCountriesAndCities_DeriveFromUniversities()
    {
        var json = """
        [
          {"Name":"A","Country":"Bulgaria","City":"Sofia"},
          {"Name":"B","Country":"Bulgaria","City":"Varna"}
        ]
        """;
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        var countries = await service.GetCountriesAsync();
        var cities = await service.GetCitiesAsync();

        Assert.Single(countries);
        Assert.Equal(2, cities.Count);
    }

    [Fact]
    public async Task GetOrCreateCountryAndCity_ReturnStubObjects()
    {
        var service = TestServiceFactory.CreateSupabaseService();
        var country = await service.GetOrCreateCountryAsync("Bulgaria");
        var city = await service.GetOrCreateCityAsync("Sofia", 1);

        Assert.Equal("Bulgaria", country.Name);
        Assert.Equal("Sofia", city.Name);
        Assert.Equal(1, city.CountryId);
    }

    [Fact]
    public async Task GetProgramsAsync_OnFailure_ReturnsEmpty()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Assert.Empty(await service.GetProgramsAsync(SampleId));
    }

    [Fact]
    public async Task GetSubjectNamesOfferedByUniversitiesAsync_ReturnsDistinctNames()
    {
        var subjectId = Guid.NewGuid();
        var json =
            "[{\"SubjectId\":\"" + subjectId + "\",\"Subject\":{\"Name\":\"Math\"}}," +
            "{\"SubjectId\":\"" + subjectId + "\",\"Subject\":{\"Name\":\"Math\"}}]";
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        var names = await service.GetSubjectNamesOfferedByUniversitiesAsync();

        Assert.Single(names);
        Assert.Equal("Math", names[0]);
    }

    [Fact]
    public async Task InsertAndPatchSubject_WorkWithMockedHttp()
    {
        var subjectId = Guid.NewGuid();
        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsoluteUri.Contains("Subjects"))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{subjectId}}","Name":"Physics"}]""");
            if (req.Method == HttpMethod.Patch)
                return FakeHttpMessageHandler.JsonResponse("[]");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var inserted = await service.InsertSubjectAsync(new Subject { Name = "Physics" });
        Assert.NotNull(inserted);
        Assert.True(await service.PatchSubjectCategoryIdAsync(subjectId, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetSubjectCategoriesAsync_ReturnsCategories()
    {
        var json = """[{"Id":"11111111-1111-1111-1111-111111111111","Name":"Tech"}]""";
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        var categories = await service.GetSubjectCategoriesAsync();

        Assert.Single(categories);
        Assert.Equal("Tech", categories[0].Name);
    }

    [Fact]
    public async Task TrackUniversityVisitAsync_DoesNotThrow()
    {
        var service = TestServiceFactory.CreateSupabaseService(req =>
            req.Method == HttpMethod.Post
                ? FakeHttpMessageHandler.JsonResponse("[]")
                : FakeHttpMessageHandler.JsonResponse("[]"));

        await service.TrackUniversityVisitAsync("u1", SampleId);
    }

    [Fact]
    public async Task GetRecentUniversityVisitsAsync_ReturnsUniversities()
    {
        var json = $$"""
        [
          {
            "universities": {
              "Id": "{{SampleId}}",
              "Name": "TU",
              "City": "Sofia",
              "Country": "BG"
            }
          }
        ]
        """;
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse(json));

        var recent = await service.GetRecentUniversityVisitsAsync("u1", 5);

        Assert.Single(recent);
    }
}
