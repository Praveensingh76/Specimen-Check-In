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
        public async Task GetManifests_ShouldReturnBadRequest_WhenTenantIdIsEmpty()
        {
            // Arrange
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(p => p.TenantId).Returns(Guid.Empty);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options, tenantProviderMock.Object))
            {
                var controller = new ManifestsController(context, tenantProviderMock.Object);

                // Act
                var result = await controller.GetManifests();

                // Assert
                result.Result.Should().BeOfType<BadRequestObjectResult>();
            }
        }

        [Fact]
        public async Task GetManifests_ShouldReturnManifests_ForActiveTenant()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(p => p.TenantId).Returns(tenantId);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                context.Manifests.Add(new Manifest { Id = Guid.NewGuid(), ManifestNumber = "M-100", TenantId = tenantId, SenderName = "Courier A" });
                context.SaveChanges();
            }

            using (var context = new ApplicationDbContext(options, tenantProviderMock.Object))
            {
                var controller = new ManifestsController(context, tenantProviderMock.Object);

                // Act
                var result = await controller.GetManifests();

                // Assert
                var okResult = result.Value.Should().NotBeNull().And.BeAssignableTo<IEnumerable<Manifest>>().Subject;
                okResult.Should().HaveCount(1);
                okResult.First().ManifestNumber.Should().Be("M-100");
            }
        }
    }
}
