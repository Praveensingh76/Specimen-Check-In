using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpecimenCheckIn.Api.Models;
using SpecimenCheckIn.Api.Services;

namespace SpecimenCheckIn.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ICurrentLabContext? _labContext;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentLabContext? labContext = null) : base(options)
        {
            _labContext = labContext;
        }

        public DbSet<Lab> Labs { get; set; } = null!;
        public DbSet<Manifest> Manifests { get; set; } = null!;
        public DbSet<Specimen> Specimens { get; set; } = null!;
        public DbSet<Discrepancy> Discrepancies { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Lab
            modelBuilder.Entity<Lab>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Name).IsRequired().HasMaxLength(150);
            });

            // Configure Manifest
            modelBuilder.Entity<Manifest>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Code).IsRequired().HasMaxLength(100);
                entity.Property(m => m.SourceClinic).IsRequired().HasMaxLength(150);
                
                // Store Enum as string
                entity.Property(m => m.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                // Relationship to Lab
                entity.HasOne(m => m.Lab)
                    .WithMany(l => l.Manifests)
                    .HasForeignKey(m => m.LabId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Global Query Filter for Lab multi-tenancy
                entity.HasQueryFilter(m => m.LabId == (_labContext != null ? _labContext.LabId : Guid.Empty));
            });

            // Configure Specimen
            modelBuilder.Entity<Specimen>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Code).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Patient).IsRequired().HasMaxLength(150);
                entity.Property(s => s.Site).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Provider).IsRequired().HasMaxLength(150);
                entity.Property(s => s.ReceivedBy).HasMaxLength(150);

                // Store Enum as string
                entity.Property(s => s.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                // Relationship to Manifest
                entity.HasOne(s => s.Manifest)
                    .WithMany(m => m.Specimens)
                    .HasForeignKey(s => s.ManifestId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Cascaded Query Filter using navigation
                entity.HasQueryFilter(s => s.Manifest != null && s.Manifest.LabId == (_labContext != null ? _labContext.LabId : Guid.Empty));
            });

            // Configure Discrepancy
            modelBuilder.Entity<Discrepancy>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Notes).HasMaxLength(500);

                // Store Enums as strings
                entity.Property(d => d.Type)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(d => d.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                // Relationship to Manifest
                entity.HasOne(d => d.Manifest)
                    .WithMany(m => m.Discrepancies)
                    .HasForeignKey(d => d.ManifestId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship to Specimen (optional, restrict cascade paths)
                entity.HasOne(d => d.Specimen)
                    .WithMany()
                    .HasForeignKey(d => d.SpecimenId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Cascaded Query Filter using navigation
                entity.HasQueryFilter(d => d.Manifest != null && d.Manifest.LabId == (_labContext != null ? _labContext.LabId : Guid.Empty));
            });
        }

        public override int SaveChanges()
        {
            ApplyLabId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyLabId();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyLabId()
        {
            if (_labContext == null) return;
            var activeLabId = _labContext.LabId;
            
            foreach (var entry in ChangeTracker.Entries<Manifest>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.LabId == Guid.Empty)
                    {
                        entry.Entity.LabId = activeLabId;
                    }
                }
            }
        }
    }
}
