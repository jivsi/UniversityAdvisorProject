using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Domain.Entities;
using AcademicProgram = UniversityAdvisor.Domain.Entities.AcademicProgram;

namespace UniversityAdvisor.Infrastructure.Data;

/// <summary>
/// Application database context using Clean Architecture
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<University> Universities { get; set; }
    public DbSet<AcademicProgram> Programs { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<SearchHistory> SearchHistories { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from Configurations folder
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

