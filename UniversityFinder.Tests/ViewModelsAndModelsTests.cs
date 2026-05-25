using UniversityFinder.DTOs;
using UniversityFinder.Models;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Tests;

public class ViewModelsAndModelsTests
{
    [Fact]
    public void SearchViewModel_Properties_RoundTrip()
    {
        var vm = new SearchViewModel
        {
            Query = "sofia",
            SubjectId = 1,
            City = "Sofia",
            DegreeType = "бакалавър",
            TotalResults = 5,
            Page = 2,
            PageSize = 10,
            Universities = [new University { Name = "TU" }],
            Cities = ["Sofia"],
            Subjects = [new Subject { Name = "Math" }]
        };

        Assert.Equal("sofia", vm.Query);
        Assert.Equal(5, vm.TotalResults);
        Assert.Single(vm.Universities);
    }

    [Fact]
    public void UniversityIndexViewModel_Properties_RoundTrip()
    {
        var vm = new UniversityIndexViewModel
        {
            Search = "tu",
            SelectedCity = "Sofia",
            SelectedSubject = "Math",
            Universities = [new University { Name = "TU" }],
            Cities = ["Sofia"],
            Subjects = ["Math"],
            RecentlyVisited = [new University { Name = "UNSS" }]
        };

        Assert.Equal("tu", vm.Search);
        Assert.Equal(2, vm.Universities.Count + vm.RecentlyVisited.Count);
    }

    [Fact]
    public void NacidDto_Models_InitializeDefaults()
    {
        var uni = new NacidUniversity();
        var spec = new NacidDetailedSpecialty { ResearchArea = new ResearchArea { Code = "1.1" } };
        var chat = new ChatRequest { Message = "hi", CityId = 3 };

        Assert.False(uni.IsActive);
        Assert.Equal("1.1", spec.ResearchArea!.Code);
        Assert.Equal("hi", chat.Message);
    }

    [Fact]
    public void ErrorViewModel_ShowsRequestIdWhenSet()
    {
        var model = new ErrorViewModel { RequestId = "trace-1" };
        Assert.True(model.ShowRequestId);
    }
}
