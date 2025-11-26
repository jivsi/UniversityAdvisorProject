using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniversityFinder.Models;

namespace UniversityFinder.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<University> Universities { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SubjectAlias> SubjectAliases { get; set; }
        public DbSet<UniversityProgram> Programs { get; set; }
        public DbSet<CostOfLiving> CostOfLiving { get; set; }
        public DbSet<CityQuality> CityQualities { get; set; }
        public DbSet<UserFavorites> UserFavorites { get; set; }
        public DbSet<SearchHistory> SearchHistory { get; set; }
        public DbSet<SyncStatus> SyncStatuses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ---------- Country ----------
            builder.Entity<Country>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Name);
            });

            // ---------- City ----------
            builder.Entity<City>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // ✅ SUPABASE POSTGRESQL: Decimal properties (Latitude, Longitude) map to numeric automatically
                // EF Core handles this correctly for PostgreSQL
                
                entity.HasIndex(e => e.CountryId);
                entity.HasIndex(e => e.Name);

                entity.HasOne(e => e.Country)
                    .WithMany(c => c.Cities)
                    .HasForeignKey(e => e.CountryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- University ----------
            builder.Entity<University>(entity =>
            {
                entity.HasKey(e => e.Id);

                // ✅ SUPABASE POSTGRESQL: Description uses text type automatically (no explicit type needed)
                // ✅ SUPABASE POSTGRESQL: Decimal properties map to numeric automatically

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Acronym)
                      .HasMaxLength(50);

                entity.Property(e => e.Website)
                      .HasMaxLength(500);

                entity.Property(e => e.HeiApiId)
                      .HasMaxLength(100);

                entity.HasIndex(e => e.CountryId);
                entity.HasIndex(e => e.CityId);
                entity.HasIndex(e => e.HeiApiId).IsUnique();
                entity.HasIndex(e => e.Name);

                entity.HasOne(e => e.Country)
                    .WithMany(c => c.Universities)
                    .HasForeignKey(e => e.CountryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.City)
                    .WithMany(c => c.Universities)
                    .HasForeignKey(e => e.CityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Subject ----------
            builder.Entity<Subject>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
            });

            // ---------- SubjectAlias ----------
            builder.Entity<SubjectAlias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SubjectId);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.LanguageCode);
                entity.HasIndex(e => new { e.SubjectId, e.Name, e.LanguageCode }).IsUnique();

                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.Aliases)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- UniversityProgram ----------
            builder.Entity<UniversityProgram>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // ✅ SUPABASE POSTGRESQL: Description uses text type automatically (no explicit type needed)
                // ✅ SUPABASE POSTGRESQL: Decimal properties map to numeric automatically
                
                // IsInferred flag: indicates if program was inferred from university name/description
                // Defaults to false (real HEI API programs)
                entity.Property(e => e.IsInferred)
                      .IsRequired()
                      .HasDefaultValue(false);
                
                entity.HasIndex(e => e.UniversityId);
                entity.HasIndex(e => e.SubjectId);
                entity.HasIndex(e => e.DegreeType);
                entity.HasIndex(e => e.IsInferred);

                entity.HasOne(e => e.University)
                    .WithMany(u => u.Programs)
                    .HasForeignKey(e => e.UniversityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.Programs)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- CostOfLiving ----------
            builder.Entity<CostOfLiving>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // ✅ SUPABASE POSTGRESQL: Decimal properties map to numeric automatically
                // EF Core handles this correctly for PostgreSQL
                
                entity.HasIndex(e => e.CityId).IsUnique();

                entity.HasOne(e => e.City)
                    .WithMany(c => c.CostOfLivingData)
                    .HasForeignKey(e => e.CityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- UserFavorites ----------
            builder.Entity<UserFavorites>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.UniversityId);
                entity.HasIndex(e => new { e.UserId, e.UniversityId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.University)
                    .WithMany(u => u.UserFavorites)
                    .HasForeignKey(e => e.UniversityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- SearchHistory ----------
            builder.Entity<SearchHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SubjectId);
                entity.HasIndex(e => e.SearchedAt);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Subject)
                    .WithMany()
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------- CityQuality ----------
            builder.Entity<CityQuality>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CityId).IsUnique();

                entity.HasOne(e => e.City)
                    .WithMany()
                    .HasForeignKey(e => e.CityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Note: City model doesn't have CityQuality navigation property to avoid circular reference
            // Access via CityQualities DbSet instead

            // ---------- SyncStatus ----------
            builder.Entity<SyncStatus>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SyncType);
                entity.HasIndex(e => e.IsRunning);
            });
        }
    }
}
