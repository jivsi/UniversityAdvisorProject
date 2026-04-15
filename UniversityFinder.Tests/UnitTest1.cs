using UniversityFinder.Services;

namespace UniversityFinder.Tests;

public class NacidScraperServiceTests
{
    [Theory]
    [InlineData("5.1.", true, 5)]
    [InlineData("05.2", true, 5)]
    [InlineData("9.3.1", true, 9)]
    [InlineData("  4.7.", true, 4)]
    [InlineData(null, false, 0)]
    [InlineData("", false, 0)]
    [InlineData("x5.1", false, 0)]
    public void TryParseResearchAreaMainNumber_ParsesLeadingDigits(string? code, bool expectedOk, int expectedMain)
    {
        var ok = NacidScraperService.TryParseResearchAreaMainNumber(code, out var main);

        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedMain, main);
    }
}
