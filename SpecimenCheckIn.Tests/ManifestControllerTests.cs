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
    }
}
