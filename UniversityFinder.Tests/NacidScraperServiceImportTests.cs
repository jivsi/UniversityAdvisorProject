using System.Net;
using System.Text.Json;
using UniversityFinder.DTOs;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests;

public class NacidScraperServiceImportTests
{
    [Fact]
    public async Task ImportDataAsync_WithNoNacidData_ReturnsZeros()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var nacid = TestServiceFactory.CreateNacidScraperService(supabase, _ =>
            FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}"""));

        var (universities, specialties) = await nacid.ImportDataAsync();

        Assert.Equal(0, universities);
        Assert.Equal(0, specialties);
    }

    [Fact]
    public async Task ImportDataAsync_SkipsInactiveUniversityAndExistingRecords()
    {
        var existingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var nacidUniversities = JsonSerializer.Serialize(new NacidUniversityResponse
        {
            Result =
            [
                new NacidUniversity { Id = 1, Name = "Existing", IsActive = true },
                new NacidUniversity { Id = 2, Name = "Inactive", IsActive = false }
            ]
        });
        var nacidSpecs = JsonSerializer.Serialize(new NacidDetailedSpecialtyResponse
        {
            Result =
            [
                new NacidDetailedSpecialty { Id = 3, Name = "Known", IsActive = true }
            ]
        });

        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (req.Method == HttpMethod.Get && uri.Contains("Name=ilike", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(
                    "[{\"Id\":\"" + existingId + "\",\"Name\":\"Existing\",\"City\":\"Sofia\",\"Country\":\"BG\"}]");
            if (req.Method == HttpMethod.Get && uri.Contains("name=eq.", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(
                    "[{\"Id\":\"" + existingId + "\",\"Name\":\"Known\"}]");
            if (req.Method == HttpMethod.Get && uri.Contains("SubjectCategories", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var nacid = TestServiceFactory.CreateNacidScraperService(supabase, req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains("Universities", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(nacidUniversities);
            if (uri.Contains("ProfessionalFields", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(nacidSpecs);
            return FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}""");
        });

        var (universities, specialties) = await nacid.ImportDataAsync();

        Assert.Equal(0, universities);
        Assert.Equal(0, specialties);
    }

    [Fact]
    public async Task ReconcileSubjectCategoriesFromNacidAsync_WithNoMatches_ReturnsZero()
    {
        var supabase = TestServiceFactory.CreateSupabaseService(req =>
        {
            var uri = req.RequestUri!.AbsoluteUri;
            if (uri.Contains("SubjectCategories", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            if (uri.Contains("/rest/v1/Subjects?", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");
            return FakeHttpMessageHandler.JsonResponse("[]");
        });

        var nacid = TestServiceFactory.CreateNacidScraperService(supabase, _ =>
            FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}"""));

        var updated = await nacid.ReconcileSubjectCategoriesFromNacidAsync();

        Assert.Equal(0, updated);
    }

    [Fact]
    public async Task ReconcileSubjectCategoriesFromNacidAsync_WithInactiveSpec_SkipsUpdate()
    {
        var nacidSpecs = JsonSerializer.Serialize(new NacidDetailedSpecialtyResponse
        {
            Result =
            [
                new NacidDetailedSpecialty
                {
                    Id = 1,
                    Name = "Pedagogy",
                    IsActive = false,
                    ResearchArea = new ResearchArea { Code = "1.2" }
                }
            ]
        });

        var supabase = TestServiceFactory.CreateSupabaseService(_ =>
            FakeHttpMessageHandler.JsonResponse("[]"));
        var nacid = TestServiceFactory.CreateNacidScraperService(supabase, req =>
        {
            if (req.RequestUri!.AbsoluteUri.Contains("ProfessionalFields", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse(nacidSpecs);
            return FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}""");
        });

        Assert.Equal(0, await nacid.ReconcileSubjectCategoriesFromNacidAsync());
    }
}
