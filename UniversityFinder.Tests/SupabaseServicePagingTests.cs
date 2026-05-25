using UniversityFinder.Models;
using UniversityFinder.Tests.TestHelpers;
using UniversityFinder.Services;

namespace UniversityFinder.Tests;

public class SupabaseServicePagingTests
{
    [Fact]
    public async Task GetSubjectNamesOfferedByUniversitiesAsync_PagesUntilPartialBatch()
    {
        var subjectId = Guid.NewGuid();
        var fullBatch = Enumerable.Range(0, 1000)
            .Select(_ => "{\"SubjectId\":\"" + subjectId + "\",\"Subject\":{\"Name\":\"Math\"}}")
            .ToArray();
        var page1 = "[" + string.Join(',', fullBatch) + "]";
        var calls = 0;

        var service = TestServiceFactory.CreateSupabaseService(req =>
        {
            if (!req.RequestUri!.AbsoluteUri.Contains("UniversityPrograms", StringComparison.OrdinalIgnoreCase))
                return FakeHttpMessageHandler.JsonResponse("[]");

            calls++;
            return FakeHttpMessageHandler.JsonResponse(calls == 1 ? page1 : "[]");
        });

        var names = await service.GetSubjectNamesOfferedByUniversitiesAsync();

        Assert.Equal(2, calls);
        Assert.Single(names);
    }
}
