using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SpecimenCheckIn.Api.Models;

namespace SpecimenCheckIn.Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, bool isDevelopment = false)
        {
            // Apply migrations automatically
            context.Database.Migrate();

            // Only seed if in Development mode
            if (!isDevelopment)
            {
                return;
            }

            // Idempotency check: skip if data already exists
            if (context.Labs.Any())
            {
                return;
            }

            // 1. Two Labs
            var riversideLab = new Lab
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Riverside Clinic"
            };

            var northgateLab = new Lab
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Northgate Derm"
            };

            context.Labs.AddRange(riversideLab, northgateLab);
            context.SaveChanges();

            // 2. Riverside Clinic Manifests

            // Manifest A (Fully Received, all specimens Received)
            var manifestA = new Manifest
            {
                Id = Guid.NewGuid(),
                LabId = riversideLab.Id,
                Code = "MNF-RIVER-001",
                Status = ManifestStatus.Closed,
                SentAt = DateTime.UtcNow.AddDays(-2),
                SourceClinic = "Riverside Main Clinic"
            };

            var patientsA = new[]
            {
                "Alice Vance", "Bob Henderson", "Charlie Miller", "Diana Ross", "Ethan Hunt", "Fiona Gallagher"
            };
            var sitesA = new[]
            {
                "Blood/Serum", "Urine", "Swab/Throat", "Blood/Plasma", "CSF", "Tissue/Biopsy"
            };

            for (int i = 0; i < patientsA.Length; i++)
            {
                var specimen = new Specimen
                {
                    Id = Guid.NewGuid(),
                    ManifestId = manifestA.Id,
                    Code = $"SP-2026-A00{41 + i}",
                    Patient = patientsA[i],
                    Site = sitesA[i],
                    Provider = "Dr. Robert Carter",
                    Status = SpecimenStatus.Received,
                    ReceivedBy = "Tech Alice",
                    ReceivedAt = DateTime.UtcNow.AddDays(-1)
                };
                manifestA.Specimens.Add(specimen);
            }

            // Manifest B (In-progress, mix of Received/Pending/Flagged to show all three states)
            var manifestB = new Manifest
            {
                Id = Guid.NewGuid(),
                LabId = riversideLab.Id,
                Code = "MNF-RIVER-002",
                Status = ManifestStatus.Open,
                SentAt = DateTime.UtcNow.AddHours(-12),
                SourceClinic = "Riverside East Wing"
            };

            var patientsB = new[]
            {
                "George Costanza", "Harold Finch", "Irene Adler", "John Watson", "Kevin Bacon", "Laura Croft", "Michael Scott"
            };
            var sitesB = new[]
            {
                "Sputum", "Blood/Serum", "Stool", "Swab/Nasal", "Urine", "Blood/EDTA", "Tissue/Skin"
            };
            var statusesB = new[]
            {
                SpecimenStatus.Received, SpecimenStatus.Pending, SpecimenStatus.Received, 
                SpecimenStatus.Pending, SpecimenStatus.Flagged, SpecimenStatus.Received, SpecimenStatus.Pending
            };

            for (int i = 0; i < patientsB.Length; i++)
            {
                var specimen = new Specimen
                {
                    Id = Guid.NewGuid(),
                    ManifestId = manifestB.Id,
                    Code = $"SP-2026-A00{51 + i}",
                    Patient = patientsB[i],
                    Site = sitesB[i],
                    Provider = "Dr. Beverly Crusher",
                    Status = statusesB[i],
                    ReceivedBy = statusesB[i] != SpecimenStatus.Pending ? "Tech Alice" : null,
                    ReceivedAt = statusesB[i] != SpecimenStatus.Pending ? DateTime.UtcNow.AddHours(-2) : null
                };
                manifestB.Specimens.Add(specimen);
            }

            // 3. Northgate Derm Manifest
            // Manifest C (One manifest with a Missing discrepancy already raised)
            var manifestC = new Manifest
            {
                Id = Guid.NewGuid(),
                LabId = northgateLab.Id,
                Code = "MNF-NORTH-001",
                Status = ManifestStatus.Open,
                SentAt = DateTime.UtcNow.AddHours(-8),
                SourceClinic = "Northgate Dermatology Center"
            };

            var patientsC = new[]
            {
                "Nancy Drew", "Oliver Queen", "Peter Parker", "Quentin Coldwater", "Rachel Green", "Steve Rogers"
            };
            var sitesC = new[]
            {
                "Skin Scraping", "Tissue/Nail", "Blood/Serum", "Swab/Wound", "Urine", "Tissue/Punch Biopsy"
            };

            for (int i = 0; i < patientsC.Length; i++)
            {
                var specimen = new Specimen
                {
                    Id = Guid.NewGuid(),
                    ManifestId = manifestC.Id,
                    Code = $"SP-2026-B00{11 + i}",
                    Patient = patientsC[i],
                    Site = sitesC[i],
                    Provider = "Dr. Stephen Strange",
                    Status = i == 2 ? SpecimenStatus.Flagged : SpecimenStatus.Pending // The 3rd specimen is missing/flagged
                };
                manifestC.Specimens.Add(specimen);
            }

            // Create a discrepancy for the 3rd specimen (Peter Parker) which is marked Missing
            var missingSpecimen = manifestC.Specimens[2];
            var discrepancy = new Discrepancy
            {
                Id = Guid.NewGuid(),
                ManifestId = manifestC.Id,
                SpecimenId = missingSpecimen.Id,
                Type = DiscrepancyType.Missing,
                Status = DiscrepancyStatus.Open,
                Notes = $"Specimen {missingSpecimen.Code} for patient {missingSpecimen.Patient} was not received in the physical shipment."
            };
            manifestC.Discrepancies.Add(discrepancy);

            context.Manifests.AddRange(manifestA, manifestB, manifestC);
            context.SaveChanges();
        }
    }
}
