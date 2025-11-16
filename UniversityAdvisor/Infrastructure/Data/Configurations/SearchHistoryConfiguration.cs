using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Infrastructure.Data.Configurations;

public class SearchHistoryConfiguration : IEntityTypeConfiguration<SearchHistory>
{
    public void Configure(EntityTypeBuilder<SearchHistory> builder)
    {
        builder.ToTable("search_history");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450).IsRequired();
        builder.Property(e => e.SearchQuery).HasColumnName("search_query").IsRequired().HasMaxLength(500);
        builder.Property(e => e.FiltersApplied).HasColumnName("filters_applied");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

        // Relationship
        builder.HasOne(e => e.User)
            .WithMany(u => u.SearchHistories)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt);
        
        // Filter for soft delete queries
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

