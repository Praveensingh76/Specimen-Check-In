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

        // Test 4: Tenant isolation on list
        [Fact]
        public async Task GetManifests_ShouldOnlyReturnActiveLabManifests_WhenMultipleLabsExist()
        {
            // Arrange
            var labIdA = Guid.NewGuid();
            var labIdB = Guid.NewGuid();
            
            var labContextMock = new Mock<ICurrentLabContext>();
            labContextMock.Setup(p => p.LabId).Returns(labIdA); // Current context is Lab A

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                context.Manifests.Add(new Manifest { Id = Guid.NewGuid(), Code = "MNF-LAB-A", LabId = labIdA, SourceClinic = "Clinic A" });
                context.Manifests.Add(new Manifest { Id = Guid.NewGuid(), Code = "MNF-LAB-B", LabId = labIdB, SourceClinic = "Clinic B" });
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
                okResult.First().Code.Should().Be("MNF-LAB-A");
                okResult.First().LabId.Should().Be(labIdA);
            }
        }

        // Test 3: Tenant isolation on GetManifest
        [Fact]
        public async Task GetManifest_ShouldReturnNotFound_WhenManifestBelongsToDifferentLab()
        {
            // Arrange
            var labIdA = Guid.NewGuid();
            var labIdB = Guid.NewGuid();
            
            var labContextMock = new Mock<ICurrentLabContext>();
            labContextMock.Setup(p => p.LabId).Returns(labIdB); // Set context to Lab B

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var manifestIdA = Guid.NewGuid();

            using (var context = new ApplicationDbContext(options))
            {
                // Seed manifest belonging to Lab A
                context.Manifests.Add(new Manifest { Id = manifestIdA, Code = "MNF-LAB-A", LabId = labIdA, SourceClinic = "Clinic A" });
                context.SaveChanges();
            }

            using (var context = new ApplicationDbContext(options, labContextMock.Object))
            {
                var controller = new ManifestsController(context, labContextMock.Object);

                // Act
                var result = await controller.GetManifest(manifestIdA);

                // Assert
                result.Result.Should().BeOfType<NotFoundObjectResult>();
            }
        }

        // Test 1: Reconciliation (Closing manifest fails with pending specimens)
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

        // Test 1: Reconciliation (Closing manifest succeeds when fully received)
        [Fact]
        public async Task CloseManifest_ShouldSucceedAndSetStatusClosed_WhenFullyReceived()
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
                var manifest = new Manifest { Id = manifestId, Code = "MNF-103", LabId = labId, SourceClinic = "Clinic Z", Status = ManifestStatus.Open };
                var specimen = new Specimen { Id = Guid.NewGuid(), ManifestId = manifestId, Code = "SP-03", Patient = "Sam", Site = "Swab", Provider = "Dr. D", Status = SpecimenStatus.Received };
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
                result.Should().BeOfType<OkObjectResult>();
                var okResult = result as OkObjectResult;
                var updatedManifest = okResult!.Value.Should().BeOfType<Manifest>().Subject;
                updatedManifest.Status.Should().Be(ManifestStatus.Closed);
            }
        }

        // Test 2: Idempotent receive
        [Fact]
        public async Task ReceiveSpecimen_ShouldBeIdempotent_WhenCalledTwice()
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
                var manifest = new Manifest { Id = manifestId, Code = "MNF-104", LabId = labId, SourceClinic = "Clinic X" };
                var specimen = new Specimen { Id = specimenId, ManifestId = manifestId, Code = "SP-04", Patient = "Joe", Site = "Serum", Provider = "Dr. P", Status = SpecimenStatus.Pending };
                context.Manifests.Add(manifest);
                context.Specimens.Add(specimen);
                context.SaveChanges();
            }

            using (var context = new ApplicationDbContext(options, labContextMock.Object))
            {
                var controller = new ManifestsController(context, labContextMock.Object);
                var request = new ReceiveSpecimenRequest { ReceivedBy = "Tech Alice" };

                // Act - First Receive call
                var result1 = await controller.ReceiveSpecimen(manifestId, specimenId, request);
                
                result1.Should().BeOfType<OkObjectResult>();
                var okResult1 = result1 as OkObjectResult;
                var updatedSpecimen1 = okResult1!.Value.Should().BeOfType<Specimen>().Subject;
                
                var firstReceivedAt = updatedSpecimen1.ReceivedAt;
                firstReceivedAt.Should().NotBeNull();

                // Wait briefly to show timestamp doesn't change
                await Task.Delay(10);

                // Act - Second Receive call
                var result2 = await controller.ReceiveSpecimen(manifestId, specimenId, request);

                // Assert
                result2.Should().BeOfType<OkObjectResult>();
                var okResult2 = result2 as OkObjectResult;
                var updatedSpecimen2 = okResult2!.Value.Should().BeOfType<Specimen>().Subject;
                
                updatedSpecimen2.Status.Should().Be(SpecimenStatus.Received);
                updatedSpecimen2.ReceivedAt.Should().Be(firstReceivedAt); // Timestamp must not have changed!
            }
        }
    }
}
