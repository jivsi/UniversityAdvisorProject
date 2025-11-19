using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Infrastructure.Data.Configurations;

public class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("programs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UniversityId).HasColumnName("university_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(500);
        builder.Property(e => e.DegreeType).HasColumnName("degree_type").IsRequired().HasMaxLength(100);
        builder.Property(e => e.DurationYears).HasColumnName("duration_years").HasPrecision(3, 1);
        builder.Property(e => e.Language).HasColumnName("language").HasMaxLength(50).HasDefaultValue("English");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(e => e.University)
            .WithMany(u => u.Programs)
            .HasForeignKey(e => e.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
