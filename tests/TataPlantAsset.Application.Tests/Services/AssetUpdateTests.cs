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
    public class AssetUpdateTests
    {
        private readonly DbContextFixture _fixture;
        private readonly Mock<ILogger<AssetHierarchyService>> _mockLogger;
        
        public AssetUpdateTests()
        {
            _fixture = new DbContextFixture();
            _mockLogger = new Mock<ILogger<AssetHierarchyService>>();
        }

        [Fact]
        public async Task UpdateAssetName_Should_Work()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var insertDto = new InsertionAssetDto { Name = "OriginalName" };
            await service.InsertAssetAsync(insertDto);

            var asset = await context.Assets.FirstAsync(a => a.Name == "OriginalName");

            var updateDto = new UpdateAssetDto
            {
                AssetId = asset.AssetId,
                OldParentId = asset.ParentId ?? Guid.Empty,
                NewParentId = asset.ParentId ?? Guid.Empty,
                OldName = "OriginalName",
                NewName = "UpdatedName"
            };

            var (isUpdated, message) = await service.UpdateAsset(updateDto);

            isUpdated.Should().BeTrue();
            var updatedAsset = await context.Assets.FindAsync(asset.AssetId);
            updatedAsset.Name.Should().Be("UpdatedName");
        }

        [Fact]
        public async Task UpdateAsset_Should_ReturnFalse_When_AssetNotFound()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var updateDto = new UpdateAssetDto
            {
                AssetId = Guid.NewGuid(),
                OldParentId = Guid.Empty,
                NewParentId = Guid.Empty,
                OldName = "X",
                NewName = "Y"
            };

            var (isUpdated, message) = await service.UpdateAsset(updateDto);
            isUpdated.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsset_Should_ReturnFalse_When_MovingUnderItsOwnChild()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "Root" });
            var root = await context.Assets.FirstAsync(a => a.Name == "Root");

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "Child", ParentId = root.AssetId });
            var child = await context.Assets.FirstAsync(a => a.Name == "Child");

            var updateDto = new UpdateAssetDto
            {
                AssetId = root.AssetId,
                OldParentId = Guid.Empty,
                NewParentId = child.AssetId,
                OldName = "Root",
                NewName = "RootUpdated"
            };

            var (isUpdated, message) = await service.UpdateAsset(updateDto);
            isUpdated.Should().BeFalse();
            message.Should().Contain("Invalid move");
        }
        [Fact]
        public async Task UpdateAsset_Should_ReturnFalse_When_RenameToDuplicate()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "AssetA" });
            await service.InsertAssetAsync(new InsertionAssetDto { Name = "AssetB" });

            var assetA = await context.Assets.FirstAsync(a => a.Name == "AssetA");

            var updateDto = new UpdateAssetDto
            {
                AssetId = assetA.AssetId,
                OldParentId = assetA.ParentId ?? Guid.Empty,
                NewParentId = assetA.ParentId ?? Guid.Empty,
                OldName = "AssetA",
                NewName = "AssetB" // still 6 characters, valid length
            };

            var (isUpdated, message) = await service.UpdateAsset(updateDto);

            isUpdated.Should().BeFalse();
            message.Should().Contain("already exists");
        }

    }
}
