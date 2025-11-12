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
        public DbSet<UniversityProgram> Programs { get; set; }
        public DbSet<CostOfLiving> CostOfLiving { get; set; }
        public DbSet<UserFavorites> UserFavorites { get; set; }
        public DbSet<SearchHistory> SearchHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Country
            builder.Entity<Country>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Name);
            });

            // Configure City
            builder.Entity<City>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CountryId);
                entity.HasIndex(e => e.Name);
                entity.HasOne(e => e.Country)
                    .WithMany(c => c.Cities)
                    .HasForeignKey(e => e.CountryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure University
            builder.Entity<University>(entity =>
            {
                entity.HasKey(e => e.Id);
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

            // Configure Subject
            builder.Entity<Subject>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
            });

            // Configure Program
            builder.Entity<UniversityProgram>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UniversityId);
                entity.HasIndex(e => e.SubjectId);
                entity.HasIndex(e => e.DegreeType);
                entity.HasOne(e => e.University)
                    .WithMany(u => u.Programs)
                    .HasForeignKey(e => e.UniversityId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.Programs)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CostOfLiving
            builder.Entity<CostOfLiving>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CityId).IsUnique();
                entity.HasOne(e => e.City)
                    .WithMany(c => c.CostOfLivingData)
                    .HasForeignKey(e => e.CityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UserFavorites
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

            // Configure SearchHistory
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
        }
    }
}
