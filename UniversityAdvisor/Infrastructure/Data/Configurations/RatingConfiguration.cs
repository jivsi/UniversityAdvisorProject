using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Infrastructure.Data.Configurations;

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.ToTable("ratings");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UniversityId).HasColumnName("university_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450).IsRequired();
        builder.Property(e => e.Score).HasColumnName("score").IsRequired();
        builder.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

        // Relationships
        builder.HasOne(e => e.University)
            .WithMany(u => u.Ratings)
            .HasForeignKey(e => e.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => new { e.UniversityId, e.UserId }).IsUnique();
        builder.HasIndex(e => e.UniversityId);
        builder.HasIndex(e => e.CreatedAt);
        
        // Filter for soft delete queries
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

