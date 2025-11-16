# Phase 1 Commit Messages

## Commit 1: Fix Favorite and SearchHistory UserId Type

```
fix: Change Favorite and SearchHistory UserId from Guid to string to match Identity

- Updated Favorite.UserId and SearchHistory.UserId to string type (Identity uses string)
- Changed navigation properties from UserProfile to ApplicationUser
- Added UpdatedAt audit fields to Favorite, SearchHistory, and Rating models
- Added IsDeleted soft delete support to University and Rating models
- Added database indexes for performance (Country, City, UserId, CreatedAt)
- Added composite unique indexes to prevent duplicate favorites/ratings
- Replaced EnsureCreatedAsync with MigrateAsync for proper migration support
- Added query filters for soft delete pattern (HasQueryFilter)

Files changed:
- Models/Favorite.cs
- Models/SearchHistory.cs
- Models/Rating.cs
- Models/University.cs
- Data/ApplicationDbContext.cs
- Program.cs

Migration: Data/Migrations/YYYYMMDDHHMMSS_FixFavoriteUserIdAndAddAuditFields.cs

BREAKING CHANGE: Existing Favorite and SearchHistory records with Guid UserId will need data migration
```

## Commit 2: Add Authorization Attributes and Role-Based Access Control

```
feat: Add authorization attributes and role-based access control

- Added [Authorize] attribute to AccountController.Profile action
- Implemented role-based access control: non-admin users cannot view other users' profiles
- Added "User" role seeding in addition to "Admin" role
- Added proper authorization checks with Forbid() for unauthorized access

Files changed:
- Controllers/AccountController.cs (added using Microsoft.AspNetCore.Authorization, [Authorize] attribute, role check)
- Program.cs (seed both Admin and User roles)

This ensures proper security boundaries and prevents unauthorized profile access.
```

## Commit 3: Fix N+1 Query Performance Issues

```
perf: Fix N+1 queries in rating lookups by implementing batch loading

- Added GetAverageRatingsAsync method to IUniversityService for batch rating lookups
- Implemented batch rating query using GroupBy to load all ratings in single query
- Replaced individual GetAverageRatingAsync calls in loops with batch GetAverageRatingsAsync
- Updated UniversitiesController (POST and GET Search actions) to use batch loading
- Updated HomeController.Search to use batch loading

Performance impact: Reduces database queries from N+1 to 2 queries total (1 for universities, 1 for all ratings)

Files changed:
- Services/IUniversityService.cs (added GetAverageRatingsAsync method)
- Services/UniversityService.cs (implemented GetAverageRatingsAsync)
- Controllers/UniversitiesController.cs (replaced loops with batch calls - 2 locations)
- Controllers/HomeController.cs (replaced loop with batch call)
```

---

## Remaining Phase 1 Tasks

- [ ] 1.4 Add pagination everywhere
- [ ] 1.5 Centralize search logic into SearchService
- [ ] 1.6 Add server-side validation with FluentValidation
- [ ] 1.7 Replace HttpClient with IHttpClientFactory (partially done - UniversityService needs fix)
- [ ] 1.8 Add global exception handler and structured logging
- [ ] 1.9 Add Data Protection and move API keys to User Secrets

