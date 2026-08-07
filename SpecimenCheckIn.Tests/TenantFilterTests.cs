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
    public class TenantFilterTests
    {
        [Fact]
        public void DbContext_ShouldFilterData_ByActiveTenantId()
        {
            // Arrange
            var tenantId1 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid();

            var tenantProviderMock = new Mock<ITenantProvider>();
            // Setup provider to return tenantId1
            tenantProviderMock.Setup(p => p.TenantId).Returns(tenantId1);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Seed database
            using (var context = new ApplicationDbContext(options))
            {
                var manifest1 = new Manifest { Id = Guid.NewGuid(), ManifestNumber = "M1", TenantId = tenantId1, SenderName = "Sender 1" };
                var manifest2 = new Manifest { Id = Guid.NewGuid(), ManifestNumber = "M2", TenantId = tenantId2, SenderName = "Sender 2" };
                context.Manifests.AddRange(manifest1, manifest2);
                context.SaveChanges();
            }

            // Act
            using (var context = new ApplicationDbContext(options, tenantProviderMock.Object))
            {
                var manifests = context.Manifests.ToList();

                // Assert
                manifests.Should().HaveCount(1);
                manifests.First().ManifestNumber.Should().Be("M1");
                manifests.First().TenantId.Should().Be(tenantId1);
            }
        }

        [Fact]
        public void DbContext_ShouldAutomaticallyAssignActiveTenantId_OnSave()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(p => p.TenantId).Returns(tenantId);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Act
            using (var context = new ApplicationDbContext(options, tenantProviderMock.Object))
            {
                var manifest = new Manifest { Id = Guid.NewGuid(), ManifestNumber = "M1", SenderName = "Sender 1" };
                context.Manifests.Add(manifest);
                context.SaveChanges();
            }

            // Assert
            using (var context = new ApplicationDbContext(options))
            {
                // Ignore query filters to check underlying data
                var manifest = context.Manifests.IgnoreQueryFilters().First();
                manifest.TenantId.Should().Be(tenantId);
            }
        }
    }
}
