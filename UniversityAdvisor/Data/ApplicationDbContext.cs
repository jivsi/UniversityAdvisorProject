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
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            
            // Indexes for performance
            entity.HasIndex(e => e.ApiIdReference).IsUnique();
            entity.HasIndex(e => e.Country);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => new { e.Country, e.City });
            
            // Filter for soft delete queries
            entity.HasQueryFilter(e => !e.IsDeleted);
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
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450); // Identity user ID max length
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Relationship to ApplicationUser (Identity)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.University)
                .WithMany(u => u.Favorites)
                .HasForeignKey(e => e.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index to prevent duplicate favorites
            entity.HasIndex(e => new { e.UserId, e.UniversityId }).IsUnique();
        });

        builder.Entity<SearchHistory>(entity =>
        {
            entity.ToTable("search_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450); // Identity user ID max length
            entity.Property(e => e.SearchQuery).HasColumnName("search_query").HasMaxLength(500);
            entity.Property(e => e.FiltersApplied).HasColumnName("filters_applied");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for faster user search history queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        builder.Entity<Rating>(entity =>
        {
            entity.ToTable("ratings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450); // Identity user ID max length
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

            entity.HasOne(e => e.University)
                .WithMany(u => u.Ratings)
                .HasForeignKey(e => e.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent user deletion if they have ratings

            // Composite unique index to prevent duplicate ratings per user/university
            entity.HasIndex(e => new { e.UniversityId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UniversityId);
            entity.HasIndex(e => e.CreatedAt);
            
            // Filter for soft delete queries
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}
