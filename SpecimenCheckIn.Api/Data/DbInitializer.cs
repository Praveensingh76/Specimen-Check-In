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

            // Seed Labs if none exist
            if (context.Labs.Any())
            {
                return; // DB has been seeded
            }

            var lab1 = new Lab
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Core Pathology Lab"
            };

            var lab2 = new Lab
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Regional Diagnostics Lab"
            };

            context.Labs.AddRange(lab1, lab2);
            context.SaveChanges();

            // Seed Manifests, Specimens, and Discrepancies
            var manifest1 = new Manifest
            {
                Id = Guid.NewGuid(),
                LabId = lab1.Id,
                Code = "MNF-PATH-001",
                Status = ManifestStatus.Open,
                SentAt = DateTime.UtcNow.AddHours(-6),
                SourceClinic = "Downtown Medical Center"
            };

            var specimen1 = new Specimen
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest1.Id,
                Code = "SPEC-A1",
                Patient = "Alice Johnson",
                Site = "Blood/Serum",
                Provider = "Dr. Green",
                Status = SpecimenStatus.Received,
                ReceivedBy = "Tech Bob",
                ReceivedAt = DateTime.UtcNow.AddHours(-1)
            };

            var specimen2 = new Specimen
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest1.Id,
                Code = "SPEC-A2",
                Patient = "Charlie Brown",
                Site = "Urine",
                Provider = "Dr. Green",
                Status = SpecimenStatus.Pending
            };

            var discrepancy1 = new Discrepancy
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest1.Id,
                SpecimenId = specimen2.Id,
                Type = DiscrepancyType.Missing,
                Status = DiscrepancyStatus.Open,
                Notes = "Specimen registered on manifest but not found in courier bag."
            };

            manifest1.Specimens.AddRange(new[] { specimen1, specimen2 });
            manifest1.Discrepancies.Add(discrepancy1);

            var manifest2 = new Manifest
            {
                Id = Guid.NewGuid(),
                LabId = lab2.Id,
                Code = "MNF-PATH-002",
                Status = ManifestStatus.Closed,
                SentAt = DateTime.UtcNow.AddHours(-12),
                SourceClinic = "Valley Pediatrics"
            };

            var specimen3 = new Specimen
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest2.Id,
                Code = "SPEC-B1",
                Patient = "David Miller",
                Site = "Swab/Nasal",
                Provider = "Dr. Davis",
                Status = SpecimenStatus.Received,
                ReceivedBy = "Tech Sarah",
                ReceivedAt = DateTime.UtcNow.AddHours(-3)
            };

            manifest2.Specimens.Add(specimen3);

            context.Manifests.AddRange(manifest1, manifest2);
            context.SaveChanges();
        }
    }
}
