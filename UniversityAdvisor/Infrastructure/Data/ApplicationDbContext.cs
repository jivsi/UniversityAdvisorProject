using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // -------------------------
    // DbSets (Domain Entities)
    // -------------------------
    public DbSet<University> Universities { get; set; } = null!;
    public DbSet<AcademicProgram> Programs { get; set; } = null!;
    public DbSet<Favorite> Favorites { get; set; } = null!;
    public DbSet<Rating> Ratings { get; set; } = null!;
    public DbSet<SearchHistory> SearchHistories { get; set; } = null!;
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ============================
        // University
        // ============================
        builder.Entity<University>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Name).IsRequired().HasMaxLength(300);
            entity.Property(u => u.Country).IsRequired().HasMaxLength(100);
            entity.Property(u => u.City).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Description).HasMaxLength(2000);
            entity.Property(u => u.WebsiteUrl).HasMaxLength(500);
            entity.Property(u => u.LogoUrl).HasMaxLength(500);
            entity.Property(u => u.ApiIdReference).HasMaxLength(500);

            entity.HasIndex(u => u.Name);
            entity.HasIndex(u => u.Country);
            entity.HasIndex(u => u.City);
            entity.HasIndex(u => new { u.Country, u.City });
        });

        // ============================
        // AcademicProgram
        // ============================
        builder.Entity<AcademicProgram>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name).IsRequired().HasMaxLength(500);
            entity.Property(p => p.DegreeType).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Language).HasMaxLength(50);
            entity.Property(p => p.Description).HasMaxLength(2000);

            entity.HasOne(p => p.University)
                .WithMany(u => u.Programs)
                .HasForeignKey(p => p.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================
        // Favorite
        // ============================
        builder.Entity<Favorite>(entity =>
        {
            entity.HasKey(f => f.Id);

            entity.Property(f => f.UserId).IsRequired();

            entity.HasOne(f => f.University)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(f => new { f.UserId, f.UniversityId }).IsUnique();
        });

        // ============================
        // Rating
        // ============================
        builder.Entity<Rating>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.UserId).IsRequired();
            entity.Property(r => r.Score).IsRequired();

            entity.HasOne(r => r.University)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.UniversityId, r.UserId }).IsUnique();
            entity.HasIndex(r => r.CreatedAt);
        });

        // ============================
        // SearchHistory  (FIXED)
        // ============================
        builder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(sh => sh.Id);

            entity.Property(sh => sh.UserId)
                .IsRequired();

            entity.Property(sh => sh.SearchQuery)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(sh => sh.FiltersApplied)
                .HasMaxLength(2000);

            entity.Property(sh => sh.CreatedAt)
                .IsRequired();

            // Navigation
            entity.HasOne(sh => sh.User)
                .WithMany(u => u.SearchHistories)
                .HasForeignKey(sh => sh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(sh => sh.CreatedAt);
        });

        // ============================
        // UserProfile
        // ============================
        builder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(up => up.Id);

            entity.Property(up => up.Email)
                .IsRequired();

            entity.Property(up => up.FullName)
                .HasMaxLength(300);
        });
    }
}
