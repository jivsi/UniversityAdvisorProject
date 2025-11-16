using UniversityAdvisor.Application.DTOs;
using UniversityAdvisor.Application.Interfaces;

namespace UniversityAdvisor.Application.UseCases.Universities;

/// <summary>
/// Use case for searching universities with pagination
/// </summary>
public class SearchUniversitiesUseCase
{
    private readonly IUniversityRepository _universityRepository;
    private readonly IRatingRepository _ratingRepository;
    private readonly IFavoriteRepository _favoriteRepository;

    public SearchUniversitiesUseCase(
        IUniversityRepository universityRepository,
        IRatingRepository ratingRepository,
        IFavoriteRepository favoriteRepository)
    {
        _universityRepository = universityRepository;
        _ratingRepository = ratingRepository;
        _favoriteRepository = favoriteRepository;
    }

    public async Task<SearchResultDto> ExecuteAsync(
        string? searchQuery,
        string? country,
        string? city,
        string? degreeType,
        decimal? minTuition,
        decimal? maxTuition,
        string? sortBy,
        int page = 1,
        int pageSize = 20,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        
        var universities = await _universityRepository.SearchAsync(
            searchQuery, country, city, degreeType, minTuition, maxTuition, sortBy,
            skip, pageSize, cancellationToken);

        var totalCount = await _universityRepository.CountAsync(
            searchQuery, country, city, degreeType, minTuition, maxTuition, cancellationToken);

        var universityIds = universities.Select(u => u.Id).ToList();
        var averageRatings = await _ratingRepository.GetAverageRatingsAsync(universityIds, cancellationToken);

        var favoriteIds = new HashSet<Guid>();
        if (!string.IsNullOrEmpty(userId))
        {
            var favorites = await _favoriteRepository.GetByUserIdAsync(userId, cancellationToken);
            favoriteIds = favorites.Select(f => f.UniversityId).ToHashSet();
        }

        var universityDtos = universities.Select(u => new UniversityDto
        {
            Id = u.Id,
            Name = u.Name,
            Country = u.Country,
            City = u.City,
            Description = u.Description,
            WebsiteUrl = u.WebsiteUrl,
            LogoUrl = u.LogoUrl,
            TuitionFeeMin = u.TuitionFeeMin,
            TuitionFeeMax = u.TuitionFeeMax,
            LivingCostMonthly = u.LivingCostMonthly,
            AcceptanceRate = u.AcceptanceRate,
            StudentCount = u.StudentCount,
            FoundedYear = u.FoundedYear,
            ProfessionsOffered = u.ProfessionsOffered,
            AverageRating = averageRatings.GetValueOrDefault(u.Id),
            RatingCount = u.Ratings.Count,
            ProgramCount = u.Programs.Count,
            IsFavorite = favoriteIds.Contains(u.Id)
        }).ToList();

        return new SearchResultDto
        {
            Universities = universityDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

