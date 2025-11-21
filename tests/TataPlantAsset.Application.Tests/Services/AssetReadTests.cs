using Application.DTOs;
using FluentAssertions;
using Infrastructure.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using TataPlantAsset.Application.Tests.Fixtures;
using Xunit;
using Moq;

namespace TataPlantAsset.Application.Tests.Services
{
    public class AssetReadTests
    {
        private readonly DbContextFixture _fixture;
        private readonly Mock<ILogger<AssetHierarchyService>> _mockLogger;
    public AssetReadTests()
        {
            _fixture = new DbContextFixture();
            _mockLogger = new Mock<ILogger<AssetHierarchyService>>();
        }

        [Fact]
        public async Task GetByParentId_Should_Return_Assets()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var parentDto = new InsertionAssetDto { Name = "ParentAsset" };
            await service.InsertAssetAsync(parentDto);
            var parent = await context.Assets.FirstAsync(a => a.Name == "ParentAsset");

            var childDto = new InsertionAssetDto { Name = "ChildAsset", ParentId = parent.AssetId };
            await service.InsertAssetAsync(childDto);

            var children = await service.GetByParentIdAsync(parent.AssetId, null);

            children.Should().HaveCount(1);
            children.First().Name.Should().Be("ChildAsset");
        }

        [Fact]
        public async Task GetAssetHierarchy_Should_Return_Roots()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "Root1" });
            await service.InsertAssetAsync(new InsertionAssetDto { Name = "Root2" });

            var roots = await service.GetAssetHierarchy();

            roots.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByParentId_Should_Return_EmptyList_When_ParentNotFound()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var result = await service.GetByParentIdAsync(Guid.NewGuid(), null);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAssetHierarchy_Should_Return_Empty_When_SearchTermNoMatch()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "RootAsset" });

            var roots = await service.GetAssetHierarchy("NonExistent");

            roots.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAssets_Should_Be_CaseInsensitive()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "CaseTest" });

            var results = await service.SearchAssetsAsync("casetest");

            results.Should().HaveCount(1);
        }
    }
}
