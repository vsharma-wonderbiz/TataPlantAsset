using Application.DTOs;
using FluentAssertions;
using Infrastructure.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TataPlantAsset.Application.Tests.Fixtures;
using Xunit;
using Moq;

namespace TataPlantAsset.Application.Tests.Services
{
    public class AssetDeleteTests
    {
        private readonly DbContextFixture _fixture;
        private readonly Mock<ILogger<AssetHierarchyService>> _mockLogger;

    public AssetDeleteTests()
        {
            _fixture = new DbContextFixture();
            _mockLogger = new Mock<ILogger<AssetHierarchyService>>();
        }

        [Fact]
        public async Task DeleteAsset_Should_Set_IsDeleted()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var dto = new InsertionAssetDto { Name = "DeleteMe" };
            await service.InsertAssetAsync(dto);
            var asset = await context.Assets.FirstAsync(a => a.Name == "DeleteMe");

            // Act
            var result = await service.DeleteAsset(asset.AssetId);

            // Assert
            result.Should().BeTrue();
            var deletedAsset = await context.Assets.FindAsync(asset.AssetId);
            deletedAsset.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsset_Should_Throw_When_HasChildren()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "Root" });
            var root = await context.Assets.FirstAsync(a => a.Name == "Root");

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "Child", ParentId = root.AssetId });

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.DeleteAsset(root.AssetId));
        }

        [Fact]
        public async Task DeleteAsset_Should_ReturnFalse_When_AssetNotFound()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            // Act
            var result = await service.DeleteAsset(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsset_Should_NotDeleteTwice()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "ToDelete" });
            var asset = await context.Assets.FirstAsync(a => a.Name == "ToDelete");

            await service.DeleteAsset(asset.AssetId);

            // Act
            var result = await service.DeleteAsset(asset.AssetId);

            // Assert
            result.Should().BeFalse(); // Already deleted
        }
    }
}
