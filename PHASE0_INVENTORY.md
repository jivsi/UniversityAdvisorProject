# PHASE 0: Repository Inventory

## Current State Summary

**Framework & Runtime:**
- .NET 8.0 (SDK 9.0.305)
- ASP.NET Core MVC
- SQLite database (via Microsoft.EntityFrameworkCore.Sqlite 8.0.0)
- ASP.NET Identity for authentication

**Current Packages:**
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)
- Microsoft.EntityFrameworkCore.Sqlite (8.0.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)

**Project Structure:**
- 7 Controllers (Account, Admin, AIAdvisor, Chat, Home, Ratings, Universities)
- 7 Models (University, Rating, ApplicationUser, Favorite, UserProfile, SearchHistory, Program)
- 3 Services (UniversityService, UniversityApiService, AIChatService)
- 4 ViewModels (SearchViewModel, RegisterViewModel, LoginViewModel, UserRatingProfileViewModel)
- Custom CSS (no Bootstrap/Tailwind currently)
- Using EnsureCreatedAsync() instead of migrations

**Critical Issues Identified:**
1. Favorite.UserId is Guid but should be string (Identity uses string)
2. No EF Core migrations (using EnsureCreatedAsync)
3. N+1 queries in rating lookups
4. No pagination
5. Duplicate search logic in controllers
6. Direct HttpClient creation
7. No global exception handling
8. API keys potentially in config files
9. Missing authorization attributes
10. No structured logging

**Database:** SQLite (university_advisor.db file)

---

## Transformation Plan

Starting with PHASE 1: Critical Hardening & Fixes, then proceeding through all 7 phases systematically.

