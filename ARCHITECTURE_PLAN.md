# PHASE 1: Clean Architecture Transformation Plan

## Current State Analysis

**Current Structure:**
- Flat MVC structure (Controllers, Models, Services, Views, ViewModels)
- Direct DbContext usage in services
- No separation of concerns
- Mixed responsibilities

**Target Structure: Clean Architecture**

```
UniversityAdvisor/
├── Domain/                          # Core business entities
│   ├── Entities/
│   │   ├── University.cs
│   │   ├── Program.cs
│   │   ├── Rating.cs
│   │   ├── Favorite.cs
│   │   ├── SearchHistory.cs
│   │   └── User.cs
│   ├── ValueObjects/
│   │   ├── Address.cs
│   │   ├── TuitionRange.cs
│   │   └── MatchScore.cs
│   └── Interfaces/
│       └── IAuditable.cs
│
├── Application/                     # Use cases & business logic
│   ├── UseCases/
│   │   ├── Universities/
│   │   │   ├── SearchUniversities/
│   │   │   ├── GetUniversityDetails/
│   │   │   └── ImportUniversities/
│   │   ├── Ratings/
│   │   │   ├── SubmitRating/
│   │   │   └── GetRatings/
│   │   ├── Favorites/
│   │   │   ├── AddFavorite/
│   │   │   └── GetFavorites/
│   │   └── AIAdvisor/
│   │       └── GetRecommendation/
│   ├── DTOs/
│   │   ├── UniversityDto.cs
│   │   ├── SearchResultDto.cs
│   │   └── RecommendationDto.cs
│   ├── Interfaces/
│   │   ├── IUniversityRepository.cs
│   │   ├── IRatingRepository.cs
│   │   ├── IFavoriteRepository.cs
│   │   └── IAIAdvisorService.cs
│   └── Mappings/
│       └── AutoMapperProfiles.cs
│
├── Infrastructure/                  # External concerns
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── UniversityConfiguration.cs
│   │   │   └── RatingConfiguration.cs
│   │   └── Repositories/
│   │       ├── UniversityRepository.cs
│   │       └── RatingRepository.cs
│   ├── ExternalServices/
│   │   ├── HipoLabsApiService.cs
│   │   ├── OpenAIAdvisorService.cs
│   │   └── IAIAdvisorService.cs
│   ├── Caching/
│   │   └── RedisCacheService.cs
│   └── Logging/
│       └── SerilogConfiguration.cs
│
├── WebUI/                           # Presentation layer
│   ├── Controllers/
│   │   ├── UniversitiesController.cs
│   │   ├── RatingsController.cs
│   │   ├── FavoritesController.cs
│   │   ├── AIAdvisorController.cs
│   │   └── AdminController.cs
│   ├── ViewModels/
│   │   ├── Universities/
│   │   ├── Ratings/
│   │   └── Dashboard/
│   ├── Views/
│   │   ├── Shared/
│   │   │   ├── _Layout.cshtml
│   │   │   ├── _Navigation.cshtml
│   │   │   └── Components/
│   │   └── [Feature folders]
│   ├── wwwroot/
│   │   ├── css/
│   │   │   ├── tailwind.css
│   │   │   └── custom.css
│   │   ├── js/
│   │   │   ├── app.js
│   │   │   ├── animations.js
│   │   │   └── search.js
│   │   └── lib/
│   └── Filters/
│       └── GlobalExceptionFilter.cs
│
└── Program.cs                       # Composition root
```

## Migration Strategy

1. **Create new folder structure**
2. **Move entities to Domain/Entities**
3. **Create Application layer interfaces**
4. **Move services to Infrastructure/Repositories**
5. **Update controllers to use Application layer**
6. **Migrate views to new structure**
7. **Update Program.cs with new DI registrations**

## Key Improvements

- **Separation of Concerns**: Each layer has single responsibility
- **Dependency Inversion**: Dependencies point inward
- **Testability**: Easy to mock interfaces
- **Maintainability**: Clear boundaries
- **Scalability**: Easy to add new features

