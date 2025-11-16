using AutoMapper;
using UniversityAdvisor.Application.DTOs;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Application.Mappings;

/// <summary>
/// AutoMapper profile for entity to DTO mappings
/// </summary>
public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<University, UniversityDto>()
            .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
            .ForMember(dest => dest.RatingCount, opt => opt.MapFrom(src => src.Ratings.Count))
            .ForMember(dest => dest.ProgramCount, opt => opt.MapFrom(src => src.Programs.Count))
            .ForMember(dest => dest.IsFavorite, opt => opt.Ignore())
            .ForMember(dest => dest.MatchScore, opt => opt.Ignore());

        CreateMap<Rating, RatingDto>();
        CreateMap<Favorite, FavoriteDto>();
    }
}

public class RatingDto
{
    public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserName { get; set; }
}

public class FavoriteDto
{
    public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public UniversityDto? University { get; set; }
}

