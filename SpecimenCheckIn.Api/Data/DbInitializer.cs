using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SpecimenCheckIn.Api.Models;

namespace SpecimenCheckIn.Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Apply migrations automatically if SQL Server is available
            context.Database.Migrate();

            // Seed Tenants if none exist
            if (context.Tenants.Any())
            {
                return; // DB has been seeded
            }

            var tenant1 = new Tenant
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Northside Medical Center",
                Code = "NORTHSIDE"
            };

            var tenant2 = new Tenant
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Downtown Diagnostics Lab",
                Code = "DOWNTOWN"
            };

            context.Tenants.AddRange(tenant1, tenant2);
            context.SaveChanges();

            // We must temporarily bypass the query filter or set TenantId manually to seed records.
            // Let's seed Manifests and Specimens. Since EF Core query filters apply to reads,
            // we can just add them with manual TenantId.
            
            var manifest1 = new Manifest
            {
                Id = Guid.NewGuid(),
                ManifestNumber = "MNF-2026-0001",
                SenderName = "Dr. Jane Smith (Northside)",
                Status = "Received",
                TenantId = tenant1.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-12)
            };

            var specimen1 = new Specimen
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest1.Id,
                SpecimenNumber = "SPC-987654",
                PatientName = "John Doe",
                AccessionNumber = "ACC-001",
                CollectionDate = DateTime.UtcNow.AddDays(-1),
                Status = "CheckedIn",
                ReceivedDate = DateTime.UtcNow.AddHours(-2),
                CheckedInBy = "Lab Tech Alice",
                TenantId = tenant1.Id
            };

            var specimen2 = new Specimen
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest1.Id,
                SpecimenNumber = "SPC-987655",
                PatientName = "Robert Johnson",
                AccessionNumber = "ACC-002",
                CollectionDate = DateTime.UtcNow.AddDays(-1),
                Status = "Pending",
                TenantId = tenant1.Id
            };

            var specimen3 = new Specimen
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest1.Id,
                SpecimenNumber = "SPC-987656",
                PatientName = "Mary Williams",
                AccessionNumber = "ACC-003",
                CollectionDate = DateTime.UtcNow.AddDays(-2),
                Status = "Rejected",
                ReceivedDate = DateTime.UtcNow.AddHours(-2),
                RejectionReason = "Leakage in container",
                CheckedInBy = "Lab Tech Alice",
                TenantId = tenant1.Id
            };

            manifest1.Specimens.AddRange(new[] { specimen1, specimen2, specimen3 });

            var manifest2 = new Manifest
            {
                Id = Guid.NewGuid(),
                ManifestNumber = "MNF-2026-0002",
                SenderName = "Downtown Courier Services",
                Status = "Created",
                TenantId = tenant2.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };

            var specimen4 = new Specimen
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest2.Id,
                SpecimenNumber = "SPC-123456",
                PatientName = "Sarah Connor",
                AccessionNumber = "ACC-101",
                CollectionDate = DateTime.UtcNow.AddHours(-5),
                Status = "Pending",
                TenantId = tenant2.Id
            };

            manifest2.Specimens.Add(specimen4);

            context.Manifests.AddRange(manifest1, manifest2);
            context.SaveChanges();
        }
    }
}
