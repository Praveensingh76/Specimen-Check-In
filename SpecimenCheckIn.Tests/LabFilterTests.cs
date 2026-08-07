using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpecimenCheckIn.Api.Data;
using SpecimenCheckIn.Api.Models;
using SpecimenCheckIn.Api.Services;
using Xunit;

namespace SpecimenCheckIn.Tests
{
    public class LabFilterTests
    {
        [Fact]
        public void DbContext_ShouldFilterData_ByActiveLabId()
        {
            // Arrange
            var labId1 = Guid.NewGuid();
            var labId2 = Guid.NewGuid();

            var labContextMock = new Mock<ICurrentLabContext>();
            labContextMock.Setup(p => p.LabId).Returns(labId1);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Seed database
            using (var context = new ApplicationDbContext(options))
            {
                var manifest1 = new Manifest { Id = Guid.NewGuid(), Code = "M1", LabId = labId1, SourceClinic = "Clinic 1" };
                var manifest2 = new Manifest { Id = Guid.NewGuid(), Code = "M2", LabId = labId2, SourceClinic = "Clinic 2" };
                context.Manifests.AddRange(manifest1, manifest2);
                context.SaveChanges();
            }

            // Act
            using (var context = new ApplicationDbContext(options, labContextMock.Object))
            {
                var manifests = context.Manifests.ToList();

                // Assert
                manifests.Should().HaveCount(1);
                manifests.First().Code.Should().Be("M1");
                manifests.First().LabId.Should().Be(labId1);
            }
        }

        [Fact]
        public void DbContext_ShouldCascadeFilterToSpecimensAndDiscrepancies_ByActiveLabId()
        {
            // Arrange
            var labId1 = Guid.NewGuid();
            var labId2 = Guid.NewGuid();

            var labContextMock = new Mock<ICurrentLabContext>();
            labContextMock.Setup(p => p.LabId).Returns(labId1);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Seed database
            using (var context = new ApplicationDbContext(options))
            {
                var manifest1 = new Manifest { Id = Guid.NewGuid(), Code = "M1", LabId = labId1, SourceClinic = "Clinic 1" };
                var manifest2 = new Manifest { Id = Guid.NewGuid(), Code = "M2", LabId = labId2, SourceClinic = "Clinic 2" };
                
                var specimen1 = new Specimen { Id = Guid.NewGuid(), ManifestId = manifest1.Id, Code = "S1", Patient = "P1", Site = "S1", Provider = "Pr1" };
                var specimen2 = new Specimen { Id = Guid.NewGuid(), ManifestId = manifest2.Id, Code = "S2", Patient = "P2", Site = "S2", Provider = "Pr2" };

                var discrepancy1 = new Discrepancy { Id = Guid.NewGuid(), ManifestId = manifest1.Id, Type = DiscrepancyType.Missing, Status = DiscrepancyStatus.Open };
                var discrepancy2 = new Discrepancy { Id = Guid.NewGuid(), ManifestId = manifest2.Id, Type = DiscrepancyType.Missing, Status = DiscrepancyStatus.Open };

                context.Manifests.AddRange(manifest1, manifest2);
                context.Specimens.AddRange(specimen1, specimen2);
                context.Discrepancies.AddRange(discrepancy1, discrepancy2);
                context.SaveChanges();
            }

            // Act
            using (var context = new ApplicationDbContext(options, labContextMock.Object))
            {
                var specimens = context.Specimens.ToList();
                var discrepancies = context.Discrepancies.ToList();

                // Assert
                specimens.Should().HaveCount(1);
                specimens.First().Code.Should().Be("S1");

                discrepancies.Should().HaveCount(1);
            }
        }
    }
}
