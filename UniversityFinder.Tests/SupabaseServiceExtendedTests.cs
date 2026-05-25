using System.Net;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests;

public class SupabaseServiceExtendedTests
{
    private static readonly Guid SampleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SubjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task GetUniversityByNameAsync_WithQuotes_UsesFallback()
    {
        var callCount = 0;
        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            callCount++;
            if (callCount == 1)
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            return FakeHttpMessageHandler.JsonResponse($$"""
            [{"Id":"{{SampleId}}","Name":"Quoted Uni","City":"Sofia"}]
            """);
        });

        var result = await service.GetUniversityByNameAsync("\"Quoted Uni\"");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUniversitiesByExactSpecialtyAsync_EmptyName_ReturnsEmpty()
    {
        var service = TestServiceFactory.CreateSupabaseService();
        Assert.Empty(await service.GetUniversitiesByExactSpecialtyAsync(""));
    }

    [Fact]
    public async Task UpdateUniversityAsync_ReturnsUpdated()
    {
        var service = TestServiceFactory.CreateSupabaseService(req =>
            req.Method == HttpMethod.Patch
                ? FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{SampleId}}","Name":"Updated","City":"Sofia","Country":"BG"}]""")
                : FakeHttpMessageHandler.JsonResponse("[]"));

        var updated = await service.UpdateUniversityAsync(SampleId, new University
        {
            Name = "Updated",
            City = "Sofia",
            Country = "BG"
        });

        Assert.Equal("Updated", updated?.Name);
    }

    [Fact]
    public async Task GetUniversityByIdAsync_ReturnsUniversity()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{SampleId}}","Name":"TU","City":"Sofia","Country":"BG"}]"""));

        var uni = await service.GetUniversityByIdAsync(SampleId);

        Assert.Equal("TU", uni?.Name);
    }

    [Fact]
    public async Task SyncUniversitiesAsync_UpdatesExistingUniversity()
    {
        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.RequestUri!.AbsoluteUri.Contains("Name=eq.", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{SampleId}}","Name":"Existing","City":"Sofia","Country":"BG"}]""");
            if (req.Method == HttpMethod.Patch)
                return FakeHttpMessageHandler.JsonResponse("[]");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        await service.SyncUniversitiesAsync(
        [
            new University { Name = "Existing", Country = "BG", City = "Sofia" }
        ]);
    }

    [Fact]
    public async Task ToggleFavoriteAsync_AddsFavoriteWhenMissing()
    {
        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.Method == HttpMethod.Get)
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (req.Method == HttpMethod.Post)
                return FakeHttpMessageHandler.JsonResponse("""[{"Id":"11111111-1111-1111-1111-111111111111"}]""");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        Assert.True(await service.ToggleFavoriteAsync("u1", 5));
    }

    [Fact]
    public async Task GetProgramsAsync_ReturnsPrograms()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""
            [{"Id":"33333333-3333-3333-3333-333333333333","UniversityId":"{{SampleId}}","SubjectId":"{{SubjectId}}","DegreeType":"бакалавър"}]
            """));

        var programs = await service.GetProgramsAsync(SampleId);

        Assert.Single(programs);
    }

    [Fact]
    public async Task InsertProgramAsync_ReturnsProgram()
    {
        var service = TestServiceFactory.CreateSupabaseService(req =>
            req.Method == HttpMethod.Post
                ? FakeHttpMessageHandler.JsonResponse($$"""
                  [{"Id":"33333333-3333-3333-3333-333333333333","UniversityId":"{{SampleId}}","SubjectId":"{{SubjectId}}"}]
                  """)
                : FakeHttpMessageHandler.JsonResponse("[]"));

        var program = await service.InsertProgramAsync(new UniversityProgram
        {
            UniversityId = SampleId,
            SubjectId = SubjectId,
            DegreeType = "бакалавър"
        });

        Assert.NotNull(program);
    }

    [Fact]
    public async Task DeleteAndUpdateUniversityProgram_Work()
    {
        var programId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (req.Method == HttpMethod.Delete)
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            if (req.Method == HttpMethod.Patch)
                return FakeHttpMessageHandler.JsonResponse("[]");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        Assert.True(await service.DeleteUniversityProgramAsync(programId));
        Assert.True(await service.UpdateUniversityProgramAsync(programId, "магистър", 2, "Редовно"));
    }

    [Fact]
    public async Task GetSubjectsAsync_FiltersByName()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{SubjectId}}","Name":"Physics"}]"""));

        var subjects = await service.GetSubjectsAsync("Physics");

        Assert.Single(subjects);
    }

    [Fact]
    public async Task GetAllSubjectsWithCategoryAsync_PagesResults()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{SubjectId}}","Name":"Physics"}]"""));

        var subjects = await service.GetAllSubjectsWithCategoryAsync();

        Assert.Single(subjects);
    }

    [Fact]
    public async Task GetSubjectsByCategoryIdAsync_ReturnsSubjects()
    {
        var categoryId = Guid.NewGuid();
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{SubjectId}}","Name":"Physics","CategoryId":"{{categoryId}}"}]"""));

        var subjects = await service.GetSubjectsByCategoryIdAsync(categoryId);

        Assert.Single(subjects);
    }

    [Fact]
    public async Task GetSubjectCategoryByIdAsync_ReturnsCategory()
    {
        var categoryId = Guid.NewGuid();
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse($$"""[{"Id":"{{categoryId}}","Name":"Tech"}]"""));

        var category = await service.GetSubjectCategoryByIdAsync(categoryId);

        Assert.Equal("Tech", category?.Name);
    }

    [Fact]
    public async Task InsertSubjectCategoryAsync_OnFailure_ReturnsNull()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        var result = await service.InsertSubjectCategoryAsync(new SubjectCategory { Name = "New" });

        Assert.Null(result);
    }

    [Fact]
    public async Task PatchSubjectCategoryIdAsync_OnFailure_ReturnsFalse()
    {
        var service = TestServiceFactory.CreateSupabaseService(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        Assert.False(await service.PatchSubjectCategoryIdAsync(SubjectId, Guid.NewGuid()));
    }
}
