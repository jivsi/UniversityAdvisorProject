using UniversityFinder.Models;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Services
{
    public interface IUniversitySearchService
    {
        Task<SearchResult> SearchAsync(SearchViewModel searchViewModel);
        Task<IEnumerable<Subject>> GetSubjectsAsync();
        Task<IEnumerable<Country>> GetCountriesAsync();
        Task<IEnumerable<City>> GetCitiesByCountryAsync(int countryId);
    }

    public class SearchResult
    {
        public List<University> Universities { get; set; } = new();
        public int TotalResults { get; set; }
    }
}

