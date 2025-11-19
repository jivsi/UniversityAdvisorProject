using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UniversityAdvisor.Application.Interfaces;

public interface ICountryRepository
{
    Task<IEnumerable<string>> GetCountriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCitiesByCountryAsync(string country, CancellationToken cancellationToken = default);
}
