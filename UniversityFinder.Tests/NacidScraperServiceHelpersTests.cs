using UniversityFinder.Services;

namespace UniversityFinder.Tests;

public class NacidScraperServiceHelpersTests
{
    [Theory]
    [InlineData("Бакалавър", "бакалавър")]
    [InlineData(" магистър ", "магистър")]
    [InlineData("ДОКТОР", "доктор")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void NormalizeDegreeType_NormalizesToLowerTrimOrEmpty(string? input, string expected)
    {
        var actual = NacidScraperService.NormalizeDegreeType(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Редовно", "Редовно")]
    [InlineData("редовно", "Редовно")]
    [InlineData("  Задочно ", "Задочно")]
    [InlineData("задочно", "Задочно")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("вечерно", "")]
    public void NormalizeStudyForm_NormalizesKnownValues(string? input, string expected)
    {
        var actual = NacidScraperService.NormalizeStudyForm(input);
        Assert.Equal(expected, actual);
    }
}

