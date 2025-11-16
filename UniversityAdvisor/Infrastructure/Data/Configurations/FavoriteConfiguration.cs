using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Infrastructure.Data.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("favorites");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450).IsRequired();
        builder.Property(e => e.UniversityId).HasColumnName("university_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.University)
            .WithMany(u => u.Favorites)
            .HasForeignKey(e => e.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite unique index to prevent duplicate favorites
        builder.HasIndex(e => new { e.UserId, e.UniversityId }).IsUnique();
        
        // Filter for soft delete queries
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

