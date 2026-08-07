using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpecimenCheckIn.Api.Controllers;
using SpecimenCheckIn.Api.Data;
using SpecimenCheckIn.Api.Models;
using SpecimenCheckIn.Api.Services;
using Xunit;

namespace SpecimenCheckIn.Tests
{
    public class ManifestControllerTests
    {
        [Fact]
        public async Task GetManifests_ShouldReturnBadRequest_WhenLabIdIsEmpty()
        {
            // Arrange
            var labContextMock = new Mock<ICurrentLabContext>();
            labContextMock.Setup(p => p.LabId).Returns(Guid.Empty);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options, labContextMock.Object))
            {
                var controller = new ManifestsController(context, labContextMock.Object);

                // Act
                var result = await controller.GetManifests();

                // Assert
                result.Result.Should().BeOfType<BadRequestObjectResult>();
            }
        }

        [Fact]
        public async Task GetManifests_ShouldReturnManifests_ForActiveLab()
        {
            // Arrange
            var labId = Guid.NewGuid();
            var labContextMock = new Mock<ICurrentLabContext>();
            labContextMock.Setup(p => p.LabId).Returns(labId);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                context.Manifests.Add(new Manifest { Id = Guid.NewGuid(), Code = "MNF-100", LabId = labId, SourceClinic = "Courier A" });
                context.SaveChanges();
            }

            using (var context = new ApplicationDbContext(options, labContextMock.Object))
            {
                var controller = new ManifestsController(context, labContextMock.Object);

                // Act
                var result = await controller.GetManifests();

                // Assert
                var okResult = result.Value.Should().NotBeNull().And.BeAssignableTo<IEnumerable<Manifest>>().Subject;
                okResult.Should().HaveCount(1);
                okResult.First().Code.Should().Be("MNF-100");
            }
        }

        [Fact]
        public async Task ReceiveSpecimen_ShouldMarkReceivedAndReturnOk_WhenSpecimenExists()
        {
            // Arrange
            var labId = Guid.NewGuid();
            var labContextMock = new Mock<ICurrentLabContext>();
            labContextMock.Setup(p => p.LabId).Returns(labId);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var manifestId = Guid.NewGuid();
            var specimenId = Guid.NewGuid();

            using (var context = new ApplicationDbContext(options))
            {
                var manifest = new Manifest { Id = manifestId, Code = "MNF-101", LabId = labId, SourceClinic = "Clinic X" };
                var specimen = new Specimen { Id = specimenId, ManifestId = manifestId, Code = "SP-01", Patient = "Joe", Site = "Serum", Provider = "Dr. P", Status = SpecimenStatus.Pending };
                context.Manifests.Add(manifest);
                context.Specimens.Add(specimen);
                context.SaveChanges();
            }

            using (var context = new ApplicationDbContext(options, labContextMock.Object))
            {
                var controller = new ManifestsController(context, labContextMock.Object);
                var request = new ReceiveSpecimenRequest { ReceivedBy = "Tech Alice" };

                // Act
                var result = await controller.ReceiveSpecimen(manifestId, specimenId, request);

                // Assert
                result.Should().BeOfType<OkObjectResult>();
                var okResult = result as OkObjectResult;
                var updatedSpecimen = okResult!.Value.Should().BeOfType<Specimen>().Subject;
                updatedSpecimen.Status.Should().Be(SpecimenStatus.Received);
                updatedSpecimen.ReceivedBy.Should().Be("Tech Alice");
            }
        }

        [Fact]
        public async Task CloseManifest_ShouldReturnConflict_WhenUnresolvedItemsExist()
        {
            // Arrange
            var labId = Guid.NewGuid();
            var labContextMock = new Mock<ICurrentLabContext>();
            labContextMock.Setup(p => p.LabId).Returns(labId);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var manifestId = Guid.NewGuid();

            using (var context = new ApplicationDbContext(options))
            {
                var manifest = new Manifest { Id = manifestId, Code = "MNF-102", LabId = labId, SourceClinic = "Clinic Y", Status = ManifestStatus.Open };
                var specimen = new Specimen { Id = Guid.NewGuid(), ManifestId = manifestId, Code = "SP-02", Patient = "Sam", Site = "Swab", Provider = "Dr. D", Status = SpecimenStatus.Pending };
                context.Manifests.Add(manifest);
                context.Specimens.Add(specimen);
                context.SaveChanges();
            }

            using (var context = new ApplicationDbContext(options, labContextMock.Object))
            {
                var controller = new ManifestsController(context, labContextMock.Object);

                // Act
                var result = await controller.CloseManifest(manifestId);

                // Assert
                result.Should().BeOfType<ConflictObjectResult>();
            }
        }
    }
}
