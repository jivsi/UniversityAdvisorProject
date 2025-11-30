// LEGACY: EF Core removed - ApplicationDbContext no longer available
// using Microsoft.EntityFrameworkCore;
// using UniversityFinder.Data;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    /// <summary>
    /// LEGACY: This service uses EF Core which has been removed.
    /// TODO: Update to use SupabaseService for seeding data
    /// </summary>
    public class DataSeeder
    {
        // LEGACY: ApplicationDbContext removed - all data now in Supabase
        // private readonly ApplicationDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(
            // ApplicationDbContext context, // LEGACY: Removed - use SupabaseService instead
            ILogger<DataSeeder> logger)
        {
            // _context = context; // LEGACY: Removed
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            _logger.LogInformation("DataSeeder is disabled - EF Core removed. Use SupabaseService for seeding.");
            await Task.CompletedTask;
            
            /* LEGACY CODE - KEPT FOR REFERENCE
            try
            {
                await SeedCountriesAsync();
                await SeedSubjectsAsync();
                await _context.SaveChangesAsync();
                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding database.");
                throw;
            }
            */
        }

        private async Task SeedCountriesAsync()
        {
            // LEGACY: EF Core removed
            await Task.CompletedTask;
            
            /* LEGACY CODE - KEPT FOR REFERENCE
            if (await _context.Countries.AnyAsync())
            {
                _logger.LogInformation("Countries already seeded.");
                return;
            }

            var countries = new List<Country>
            {
                new Country { Name = "Austria", Code = "AT", Region = "Central Europe" },
                new Country { Name = "Belgium", Code = "BE", Region = "Western Europe" },
                new Country { Name = "Bulgaria", Code = "BG", Region = "Eastern Europe" },
                new Country { Name = "Croatia", Code = "HR", Region = "Southern Europe" },
                new Country { Name = "Cyprus", Code = "CY", Region = "Southern Europe" },
                new Country { Name = "Czech Republic", Code = "CZ", Region = "Central Europe" },
                new Country { Name = "Denmark", Code = "DK", Region = "Northern Europe" },
                new Country { Name = "Estonia", Code = "EE", Region = "Northern Europe" },
                new Country { Name = "Finland", Code = "FI", Region = "Northern Europe" },
                new Country { Name = "France", Code = "FR", Region = "Western Europe" },
                new Country { Name = "Germany", Code = "DE", Region = "Central Europe" },
                new Country { Name = "Greece", Code = "GR", Region = "Southern Europe" },
                new Country { Name = "Hungary", Code = "HU", Region = "Central Europe" },
                new Country { Name = "Iceland", Code = "IS", Region = "Northern Europe" },
                new Country { Name = "Ireland", Code = "IE", Region = "Northern Europe" },
                new Country { Name = "Italy", Code = "IT", Region = "Southern Europe" },
                new Country { Name = "Latvia", Code = "LV", Region = "Northern Europe" },
                new Country { Name = "Liechtenstein", Code = "LI", Region = "Central Europe" },
                new Country { Name = "Lithuania", Code = "LT", Region = "Northern Europe" },
                new Country { Name = "Luxembourg", Code = "LU", Region = "Western Europe" },
                new Country { Name = "Malta", Code = "MT", Region = "Southern Europe" },
                new Country { Name = "Netherlands", Code = "NL", Region = "Western Europe" },
                new Country { Name = "Norway", Code = "NO", Region = "Northern Europe" },
                new Country { Name = "Poland", Code = "PL", Region = "Central Europe" },
                new Country { Name = "Portugal", Code = "PT", Region = "Southern Europe" },
                new Country { Name = "Romania", Code = "RO", Region = "Eastern Europe" },
                new Country { Name = "Slovakia", Code = "SK", Region = "Central Europe" },
                new Country { Name = "Slovenia", Code = "SI", Region = "Southern Europe" },
                new Country { Name = "Spain", Code = "ES", Region = "Southern Europe" },
                new Country { Name = "Sweden", Code = "SE", Region = "Northern Europe" },
                new Country { Name = "Switzerland", Code = "CH", Region = "Central Europe" },
                new Country { Name = "United Kingdom", Code = "GB", Region = "Northern Europe" }
            };

            await _context.Countries.AddRangeAsync(countries);
            _logger.LogInformation($"Seeded {countries.Count} countries.");
            */
        }

        private async Task SeedSubjectsAsync()
        {
            // LEGACY: EF Core removed
            await Task.CompletedTask;
            
            /* LEGACY CODE - KEPT FOR REFERENCE
            if (await _context.Subjects.AnyAsync())
            {
                _logger.LogInformation("Subjects already seeded.");
                return;
            }

            var subjects = new List<Subject>
            {
                // STEM
                new Subject { Name = "Computer Science", Category = "STEM", Description = "Study of computation, information processing, and the design of computer systems." },
                new Subject { Name = "Engineering", Category = "STEM", Description = "Application of science and mathematics to design and build structures, machines, and systems." },
                new Subject { Name = "Mathematics", Category = "STEM", Description = "Study of numbers, quantities, shapes, and patterns." },
                new Subject { Name = "Physics", Category = "STEM", Description = "Study of matter, motion, energy, and force." },
                new Subject { Name = "Chemistry", Category = "STEM", Description = "Study of matter, its properties, and how substances interact." },
                new Subject { Name = "Biology", Category = "STEM", Description = "Study of living organisms and their interactions." },
                new Subject { Name = "Medicine", Category = "STEM", Description = "Science and practice of diagnosis, treatment, and prevention of disease." },
                new Subject { Name = "Architecture", Category = "STEM", Description = "Art and science of designing buildings and structures." },

                // Social Sciences
                new Subject { Name = "Psychology", Category = "Social Sciences", Description = "Scientific study of mind and behavior." },
                new Subject { Name = "Sociology", Category = "Social Sciences", Description = "Study of social behavior, social groups, and society." },
                new Subject { Name = "Political Science", Category = "Social Sciences", Description = "Study of politics, government systems, and political behavior." },
                new Subject { Name = "Economics", Category = "Social Sciences", Description = "Study of production, distribution, and consumption of goods and services." },
                new Subject { Name = "Law", Category = "Social Sciences", Description = "Study of legal systems, rules, and regulations." },
                new Subject { Name = "Education", Category = "Social Sciences", Description = "Study of teaching, learning, and educational systems." },
                new Subject { Name = "International Relations", Category = "Social Sciences", Description = "Study of relationships between countries and international organizations." },

                // Business
                new Subject { Name = "Business Administration", Category = "Business", Description = "Study of managing and operating businesses." },
                new Subject { Name = "Marketing", Category = "Business", Description = "Study of promoting and selling products or services." },
                new Subject { Name = "Finance", Category = "Business", Description = "Study of money management, investments, and financial systems." },
                new Subject { Name = "Accounting", Category = "Business", Description = "Recording, classifying, and reporting financial transactions." },
                new Subject { Name = "Management", Category = "Business", Description = "Study of organizing and coordinating business activities." },

                // Arts & Humanities
                new Subject { Name = "Literature", Category = "Arts & Humanities", Description = "Study of written works and literary analysis." },
                new Subject { Name = "History", Category = "Arts & Humanities", Description = "Study of past events and human affairs." },
                new Subject { Name = "Philosophy", Category = "Arts & Humanities", Description = "Study of fundamental questions about existence, knowledge, and values." },
                new Subject { Name = "Languages", Category = "Arts & Humanities", Description = "Study of foreign languages and linguistics." },
                new Subject { Name = "Art", Category = "Arts & Humanities", Description = "Study and practice of visual arts, design, and creative expression." },
                new Subject { Name = "Music", Category = "Arts & Humanities", Description = "Study of music theory, composition, and performance." },
                new Subject { Name = "Theater", Category = "Arts & Humanities", Description = "Study of dramatic arts and performance." },

                // Other
                new Subject { Name = "Journalism", Category = "Media & Communication", Description = "Study of news reporting, writing, and media production." },
                new Subject { Name = "Communication", Category = "Media & Communication", Description = "Study of human communication processes and media." },
                new Subject { Name = "Environmental Science", Category = "STEM", Description = "Study of the environment and solutions to environmental problems." },
                new Subject { Name = "Nursing", Category = "STEM", Description = "Healthcare profession focused on patient care and health promotion." }
            };

            await _context.Subjects.AddRangeAsync(subjects);
            _logger.LogInformation($"Seeded {subjects.Count} subjects.");
            */
        }
    }
}

