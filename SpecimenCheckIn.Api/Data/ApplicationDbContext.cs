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
        private readonly ITenantProvider? _tenantProvider;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantProvider? tenantProvider = null) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<Manifest> Manifests { get; set; } = null!;
        public DbSet<Specimen> Specimens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Tenant
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.Code).IsUnique();
                entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Code).IsRequired().HasMaxLength(20);
            });

            // Configure Manifest
            modelBuilder.Entity<Manifest>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => m.ManifestNumber).IsUnique();
                entity.Property(m => m.ManifestNumber).IsRequired().HasMaxLength(50);
                entity.Property(m => m.SenderName).IsRequired().HasMaxLength(100);
                entity.Property(m => m.Status).IsRequired().HasMaxLength(50);

                // Global Query Filter for Tenant isolation
                entity.HasQueryFilter(m => m.TenantId == (_tenantProvider != null ? _tenantProvider.TenantId : Guid.Empty));
            });

            // Configure Specimen
            modelBuilder.Entity<Specimen>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.SpecimenNumber).IsUnique();
                entity.Property(s => s.SpecimenNumber).IsRequired().HasMaxLength(50);
                entity.Property(s => s.PatientName).IsRequired().HasMaxLength(100);
                entity.Property(s => s.AccessionNumber).IsRequired().HasMaxLength(50);
                entity.Property(s => s.Status).IsRequired().HasMaxLength(50);
                entity.Property(s => s.RejectionReason).HasMaxLength(250);
                entity.Property(s => s.CheckedInBy).HasMaxLength(100);

                // Relationship: Manifest has many Specimens
                entity.HasOne(s => s.Manifest)
                    .WithMany(m => m.Specimens)
                    .HasForeignKey(s => s.ManifestId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Global Query Filter for Tenant isolation
                entity.HasQueryFilter(s => s.TenantId == (_tenantProvider != null ? _tenantProvider.TenantId : Guid.Empty));
            });
        }

        public override int SaveChanges()
        {
            ApplyTenantId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantId();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTenantId()
        {
            if (_tenantProvider == null) return;

            var currentTenantId = _tenantProvider.TenantId;
            if (currentTenantId == Guid.Empty)
            {
                // Note: If saving tenant-specific records without a TenantId, we could throw or let EF Core save it.
                // We should only enforce TenantId for actual ITenantEntity implementations when they are added.
            }

            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Only overwrite if it wasn't manually set to something else (e.g. for database seeding/tests)
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = currentTenantId;
                    }
                }
            }
        }
    }
}
