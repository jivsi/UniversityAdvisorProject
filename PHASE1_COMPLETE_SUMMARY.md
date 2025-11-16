# PHASE 1: Critical Hardening & Fixes - Complete Implementation Summary

## Status: IN PROGRESS

This document tracks all Phase 1 changes with before/after diffs and commit messages.

---

## ✅ 1.1 Fix Favorite.UserId Type - COMPLETED

**Commit:** `fix: Change Favorite and SearchHistory UserId from Guid to string to match Identity`

**Files Changed:**
- `Models/Favorite.cs` - UserId: Guid → string, Added UpdatedAt, Navigation: UserProfile → ApplicationUser
- `Models/SearchHistory.cs` - UserId: Guid → string, Added UpdatedAt, Navigation: UserProfile → ApplicationUser
- `Models/Rating.cs` - Added UpdatedAt, IsDeleted (soft delete)
- `Models/University.cs` - Added IsDeleted (soft delete)
- `Data/ApplicationDbContext.cs` - Updated FK configs, added indexes, soft delete filters
- `Program.cs` - Replaced EnsureCreatedAsync with MigrateAsync

**Migration:** `Data/Migrations/YYYYMMDDHHMMSS_FixFavoriteUserIdAndAddAuditFields.cs`

---

## ✅ 1.2 Add Authorization Attributes - COMPLETED

**Commit:** `feat: Add authorization attributes and role-based access control`

**Files Changed:**
- `Controllers/AccountController.cs` - Added [Authorize] to Profile, added role check for viewing other profiles
- `Program.cs` - Seed both "Admin" and "User" roles

**Changes:**
- Profile endpoint now requires authentication
- Non-admin users cannot view other users' profiles
- Both Admin and User roles are seeded on startup

---

## 🔄 1.3 Fix N+1 Queries - IN PROGRESS

**Issue:** Controllers loop through results and call GetAverageRatingAsync for each university individually.

**Solution:** Load all ratings in a single query and group by university ID.

**Files to Change:**
- `Services/IUniversityService.cs` - Add method to get ratings for multiple universities
- `Services/UniversityService.cs` - Implement batch rating lookup
- `Controllers/UniversitiesController.cs` - Replace loop with batch call
- `Controllers/HomeController.cs` - Replace loop with batch call

---

## 🔄 1.4 Add Pagination - PENDING

**Files to Change:**
- Create `ViewModels/PagedResult.cs` for pagination model
- Update `ViewModels/SearchViewModel.cs` - Add Page, PageSize, TotalPages, HasNextPage, HasPreviousPage
- Update `Services/IUniversityService.cs` - Change SearchUniversitiesAsync to return PagedResult
- Update `Services/UniversityService.cs` - Implement pagination in search
- Update `Controllers/UniversitiesController.cs` - Handle pagination parameters
- Create `Views/Shared/_Pagination.cshtml` - Reusable pagination component

---

## 🔄 1.5 Centralize Search Logic - PENDING

**Files to Create:**
- `Services/ISearchService.cs` - Interface for search operations
- `Services/SearchService.cs` - Centralized search logic

**Files to Change:**
- `Controllers/UniversitiesController.cs` - Use SearchService instead of direct UniversityService calls
- `Controllers/HomeController.cs` - Remove duplicate search logic, redirect to Universities controller

---

## 🔄 1.6 Add Server-Side Validation - PENDING

**Packages to Add:**
- FluentValidation.AspNetCore

**Files to Create:**
- `Validators/SearchViewModelValidator.cs`
- `Validators/RegisterViewModelValidator.cs`
- `Validators/RatingViewModelValidator.cs`

**Files to Change:**
- `Program.cs` - Register FluentValidation
- All ViewModels - Add DataAnnotations as backup

---

## 🔄 1.7 Replace HttpClient with IHttpClientFactory - PENDING

**Files to Change:**
- `Services/UniversityService.cs` - Remove direct HttpClient, use IHttpClientFactory (already injected but not used)
- `Services/UniversityApiService.cs` - Already uses IHttpClientFactory ✅
- `Services/AIChatService.cs` - Already uses IHttpClientFactory ✅

---

## 🔄 1.8 Add Global Exception Handler - PENDING

**Files to Create:**
- `Middleware/GlobalExceptionHandlerMiddleware.cs`
- `ViewModels/ErrorViewModel.cs`

**Files to Change:**
- `Program.cs` - Register middleware
- `Controllers/HomeController.cs` - Add Error action if missing

---

## 🔄 1.9 Add Data Protection and Move API Keys to Secrets - PENDING

**Files to Change:**
- `Program.cs` - Add Data Protection services
- `appsettings.json` - Remove any hardcoded API keys (add comments)
- Create `SECRETS_SETUP.md` - Instructions for User Secrets

**Commands:**
```bash
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "your-key-here"
```

---

## Next Steps

1. Complete N+1 query fixes
2. Implement pagination
3. Add global exception handler
4. Add FluentValidation
5. Move secrets to User Secrets
6. Test all changes

