using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityAdvisor.Domain.Entities;


namespace UniversityAdvisor.Infrastructure.Data.Configurations;

public class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> builder)
    {
        builder.ToTable("universities");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(500);
        builder.Property(e => e.Country).HasColumnName("country").IsRequired().HasMaxLength(100);
        builder.Property(e => e.City).HasColumnName("city").IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.WebsiteUrl).HasColumnName("website_url").HasMaxLength(500);
        builder.Property(e => e.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
        builder.Property(e => e.TuitionFeeMin).HasColumnName("tuition_fee_min").HasPrecision(18, 2);
        builder.Property(e => e.TuitionFeeMax).HasColumnName("tuition_fee_max").HasPrecision(18, 2);
        builder.Property(e => e.LivingCostMonthly).HasColumnName("living_cost_monthly").HasPrecision(18, 2);
        builder.Property(e => e.AcceptanceRate).HasColumnName("acceptance_rate").HasPrecision(5, 2);
        builder.Property(e => e.StudentCount).HasColumnName("student_count");
        builder.Property(e => e.FoundedYear).HasColumnName("founded_year");
        builder.Property(e => e.ProfessionsOffered).HasColumnName("professions_offered");
        builder.Property(e => e.ApiIdReference).HasColumnName("api_id_reference").HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        
        // Indexes for performance
        builder.HasIndex(e => e.ApiIdReference).IsUnique();
        builder.HasIndex(e => e.Country);
        builder.HasIndex(e => e.City);
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => new { e.Country, e.City });
        
        // Relationships
        builder.HasMany(e => e.Programs)
            .WithOne(p => p.University)
            .HasForeignKey(p => p.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(e => e.Favorites)
            .WithOne(f => f.University)
            .HasForeignKey(f => f.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(e => e.Ratings)
            .WithOne(r => r.University)
            .HasForeignKey(r => r.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Filter for soft delete queries
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

