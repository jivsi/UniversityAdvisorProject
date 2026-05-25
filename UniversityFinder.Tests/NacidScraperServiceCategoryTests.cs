using UniversityFinder.Models;
using UniversityFinder.Services;

namespace UniversityFinder.Tests;

public class NacidScraperServiceCategoryTests
{
    [Fact]
    public void BuildCategoryNameMap_TrimsAndDeduplicatesByName()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var categories = new List<SubjectCategory>
        {
            new() { Id = id1, Name = "  Tech  " },
            new() { Id = id2, Name = "Tech" },
            new() { Id = Guid.NewGuid(), Name = " " }
        };

        var map = NacidScraperService.BuildCategoryNameMap(categories);

        Assert.Single(map);
        Assert.Equal(id1, map["Tech"]);
    }

    [Theory]
    [InlineData("5.1.", "Технически науки")]
    [InlineData("1.0", "Педагогически науки")]
    [InlineData("9.2.1", "Сигурност и отбрана")]
    [InlineData(null, null)]
    [InlineData("99.1", null)]
    public void ResolveCategoryIdFromResearchAreaCode_MapsMainScientificAreas(string? code, string? expectedCategoryName)
    {
        var categoryId = Guid.NewGuid();
        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in NacidScraperService.MainScientificAreaNames)
            map[name] = categoryId;

        var resolved = NacidScraperService.ResolveCategoryIdFromResearchAreaCode(code, map);

        if (expectedCategoryName == null)
        {
            Assert.Null(resolved);
        }
        else
        {
            Assert.Equal(categoryId, resolved);
            Assert.Equal(expectedCategoryName, NacidScraperService.MainScientificAreaNames[int.Parse(code![..1]) - 1]);
        }
    }
}
