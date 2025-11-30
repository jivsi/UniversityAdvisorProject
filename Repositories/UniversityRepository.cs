using System;
using System.Linq;
// LEGACY: EF Core removed - ApplicationDbContext no longer available
// using Microsoft.EntityFrameworkCore;
// using UniversityFinder.Data;
using UniversityFinder.Models;
using UniversityFinder.Services;

namespace UniversityFinder.Repositories
{
    /// <summary>
    /// LEGACY: This repository uses EF Core which has been removed.
    /// TODO: Update to use SupabaseService instead of ApplicationDbContext
    /// </summary>
    public class UniversityRepository : IUniversityRepository
    {
        // LEGACY: ApplicationDbContext removed - all data now in Supabase
        // private readonly ApplicationDbContext _context;
        private readonly ISubjectNormalizationService _normalizationService;

        public UniversityRepository(
            // ApplicationDbContext context, // LEGACY: Removed - use SupabaseService instead
            ISubjectNormalizationService normalizationService)
        {
            // _context = context; // LEGACY: Removed
            _normalizationService = normalizationService;
        }

        public async Task<IEnumerable<University>> SearchBySubjectAsync(string subjectName, int? countryId = null, int? cityId = null, string? degreeType = null)
        {
            // LEGACY: EF Core removed - this method needs to be updated to use SupabaseService
            // For now, return empty to prevent build errors
            // TODO: Implement using SupabaseService
            return Enumerable.Empty<University>();
            
            /* LEGACY CODE - KEPT FOR REFERENCE
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                return Enumerable.Empty<University>();
            }

            // Build base query with includes - CRITICAL: Include SubjectAliases for language-agnostic search
            var query = _context.Universities
                .Include(u => u.Country)
                .Include(u => u.City)
                .Include(u => u.Programs)
                    .ThenInclude(p => p.Subject)
                        .ThenInclude(s => s.Aliases)
                .AsQueryable();

            // Apply location filters first
            if (countryId.HasValue)
            {
                query = query.Where(u => u.CountryId == countryId.Value);
            }

            if (cityId.HasValue)
            {
                query = query.Where(u => u.CityId == cityId.Value);
            }

            // CRITICAL: Search ONLY by programs/subjects, NOT by university name
            // This ensures language-agnostic, profession-based search
            // Normalize the search term for better matching
            var normalizedSearchTerm = _normalizationService.NormalizeSubjectName(subjectName);
            var lowerSubjectName = subjectName.ToLower().Trim();
            
            query = query.Where(u => 
                // Match programs with subjects by:
                // 1. Subject name (normalized and direct)
                // 2. Subject aliases (all language variants)
                // 3. Program name (as fallback, but not primary)
                u.Programs.Any(p => 
                    // Match against subject name
                    (_normalizationService.NormalizeSubjectName(p.Subject.Name).Contains(normalizedSearchTerm) ||
                     p.Subject.Name.Contains(subjectName, StringComparison.OrdinalIgnoreCase) ||
                     p.Subject.Name.ToLower().Contains(lowerSubjectName)) ||
                    // Match against subject aliases (all language variants)
                    p.Subject.Aliases.Any(a => 
                        _normalizationService.NormalizeSubjectName(a.Name).Contains(normalizedSearchTerm) ||
                        a.Name.Contains(subjectName, StringComparison.OrdinalIgnoreCase) ||
                        a.Name.ToLower().Contains(lowerSubjectName)) ||
                    // Fallback: check program name (but this is secondary)
                    (!string.IsNullOrEmpty(p.Name) && (
                        _normalizationService.NormalizeSubjectName(p.Name).Contains(normalizedSearchTerm) ||
                        p.Name.Contains(subjectName, StringComparison.OrdinalIgnoreCase)))
                )
            );

            // Apply degree type filter if specified
            if (!string.IsNullOrEmpty(degreeType))
            {
                query = query.Where(u => 
                    u.Programs.Any(p => 
                        p.DegreeType == degreeType &&
                        // Ensure the program also matches the subject search
                        (_normalizationService.NormalizeSubjectName(p.Subject.Name).Contains(normalizedSearchTerm) ||
                         p.Subject.Name.Contains(subjectName, StringComparison.OrdinalIgnoreCase) ||
                         p.Subject.Name.ToLower().Contains(lowerSubjectName) ||
                         // Match against aliases
                         p.Subject.Aliases.Any(a => 
                             _normalizationService.NormalizeSubjectName(a.Name).Contains(normalizedSearchTerm) ||
                             a.Name.Contains(subjectName, StringComparison.OrdinalIgnoreCase) ||
                             a.Name.ToLower().Contains(lowerSubjectName)) ||
                         // Fallback: program name
                         (!string.IsNullOrEmpty(p.Name) && (
                             _normalizationService.NormalizeSubjectName(p.Name).Contains(normalizedSearchTerm) ||
                             p.Name.Contains(subjectName, StringComparison.OrdinalIgnoreCase))))
                    ));
            }

            return await query.Distinct().ToListAsync();
            */
        }

        public async Task<University?> GetByIdAsync(int id)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return null;
        }

        public async Task<University?> GetByIdWithDetailsAsync(int id)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return null;
        }

        public async Task<IEnumerable<University>> GetAllAsync()
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return Enumerable.Empty<University>();
        }

        public async Task<IEnumerable<University>> GetByCountryAsync(int countryId)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return Enumerable.Empty<University>();
        }

        public async Task<IEnumerable<University>> GetByCityAsync(int cityId)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return Enumerable.Empty<University>();
        }
    }
}

