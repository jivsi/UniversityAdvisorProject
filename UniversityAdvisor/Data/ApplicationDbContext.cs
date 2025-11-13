using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Models;

namespace UniversityAdvisor.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<University> Universities { get; set; }
    public DbSet<Models.Program> Programs { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<SearchHistory> SearchHistories { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<University>(entity =>
        {
            entity.ToTable("universities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Country).HasColumnName("country");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.WebsiteUrl).HasColumnName("website_url");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.TuitionFeeMin).HasColumnName("tuition_fee_min");
            entity.Property(e => e.TuitionFeeMax).HasColumnName("tuition_fee_max");
            entity.Property(e => e.LivingCostMonthly).HasColumnName("living_cost_monthly");
            entity.Property(e => e.AcceptanceRate).HasColumnName("acceptance_rate");
            entity.Property(e => e.StudentCount).HasColumnName("student_count");
            entity.Property(e => e.FoundedYear).HasColumnName("founded_year");
            entity.Property(e => e.ProfessionsOffered).HasColumnName("professions_offered");
            entity.Property(e => e.ApiIdReference).HasColumnName("api_id_reference");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasIndex(e => e.ApiIdReference).IsUnique().HasFilter("\"api_id_reference\" IS NOT NULL");
        });

        builder.Entity<Models.Program>(entity =>
        {
            entity.ToTable("programs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.DegreeType).HasColumnName("degree_type");
            entity.Property(e => e.DurationYears).HasColumnName("duration_years");
            entity.Property(e => e.Language).HasColumnName("language");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.University)
                .WithMany(u => u.Programs)
                .HasForeignKey(e => e.UniversityId);
        });

        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.Country).HasColumnName("country");
            entity.Property(e => e.Interests).HasColumnName("interests");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.ToTable("favorites");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(e => e.UserId);

            entity.HasOne(e => e.University)
                .WithMany(u => u.Favorites)
                .HasForeignKey(e => e.UniversityId);
        });

        builder.Entity<SearchHistory>(entity =>
        {
            entity.ToTable("search_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SearchQuery).HasColumnName("search_query");
            entity.Property(e => e.FiltersApplied).HasColumnName("filters_applied");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.SearchHistories)
                .HasForeignKey(e => e.UserId);
        });

        builder.Entity<Rating>(entity =>
        {
            entity.ToTable("ratings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.University)
                .WithMany(u => u.Ratings)
                .HasForeignKey(e => e.UniversityId);
        });
    }
}
