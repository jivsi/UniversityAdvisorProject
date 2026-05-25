using System.Net;
using System.Text.Json;
using UniversityFinder.DTOs;
using UniversityFinder.Tests.TestHelpers;
using UniversityFinder.Services;

namespace UniversityFinder.Tests;

public class NacidScraperServiceHttpTests
{
    [Fact]
    public async Task GetUniversitiesAsync_ReturnsPagedResults()
    {
        var batch = new NacidUniversityResponse
        {
            TotalCount = 1,
            Result = [new NacidUniversity { Id = 1, Name = "SU", IsActive = true }]
        };

        var service = TestServiceFactory.CreateNacidScraperService(nacidHandler: _ =>
            FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(batch)));

        var result = await service.GetUniversitiesAsync();

        Assert.Single(result);
        Assert.Equal("SU", result[0].Name);
    }

    [Fact]
    public async Task GetUniversitiesAsync_OnHttpError_ReturnsEmptyList()
    {
        var service = TestServiceFactory.CreateNacidScraperService(nacidHandler: _ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await service.GetUniversitiesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDetailedSpecialtiesAsync_ReturnsPagedResults()
    {
        var batch = new NacidDetailedSpecialtyResponse
        {
            TotalCount = 1,
            Result =
            [
                new NacidDetailedSpecialty
                {
                    Id = 10,
                    Name = "Informatics",
                    IsActive = true,
                    ResearchArea = new ResearchArea { Code = "4.1." }
                }
            ]
        };

        var service = TestServiceFactory.CreateNacidScraperService(nacidHandler: req =>
        {
            if (req.RequestUri!.AbsoluteUri.Contains("ProfessionalFields"))
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(batch));
            return FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}""");
        });

        var result = await service.GetDetailedSpecialtiesAsync();

        Assert.Single(result);
        Assert.Equal("Informatics", result[0].Name);
    }

    [Fact]
    public async Task GetDetailedSpecialtiesAsync_OnHttpError_ReturnsEmptyList()
    {
        var service = TestServiceFactory.CreateNacidScraperService(nacidHandler: _ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Assert.Empty(await service.GetDetailedSpecialtiesAsync());
    }
}
