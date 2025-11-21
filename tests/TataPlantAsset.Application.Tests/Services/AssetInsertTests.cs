using Application.DTOs;
using FluentAssertions;
using Infrastructure.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TataPlantAsset.Application.Tests.Fixtures;
using TataPlantAsset.Application.Tests.TestData;
using Domain.Entities;
using Xunit;
using Moq;

namespace TataPlantAsset.Application.Tests.Services
{
    public class AssetInsertTests : IClassFixture<DbContextFixture>
    {
        private readonly DbContextFixture _fixture;
        private readonly Mock<ILogger<AssetHierarchyService>> _mockLogger;

    public AssetInsertTests(DbContextFixture fixture)
        {
            _fixture = fixture;
            _mockLogger = new Mock<ILogger<AssetHierarchyService>>();
        }

        [Fact]
        public async Task InsertAsset_Should_Work()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);
            var dto = AssetTestData.RootAsset;

            var result = await service.InsertAssetAsync(dto);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_LevelExceeds5()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            Guid parentId = Guid.NewGuid();
            for (int i = 1; i <= 5; i++)
            {
                await context.Assets.AddAsync(new Asset { AssetId = parentId, Name = $"Level{i}", Level = i });
                parentId = Guid.NewGuid();
            }
            await context.SaveChangesAsync();

            var dto = new InsertionAssetDto { Name = "TooDeep", ParentId = parentId };

            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(dto));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_NameTooShort()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var dto = new InsertionAssetDto { Name = "A" };

            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(dto));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_NameContainsInvalidCharacters()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var dto = new InsertionAssetDto { Name = "Invalid@Name" };

            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(dto));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_DuplicateNameUnderSameParent()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var parentDto = AssetTestData.RootAsset;
            await service.InsertAssetAsync(parentDto);

            var duplicateDto = new InsertionAssetDto
            {
                Name = parentDto.Name,
                ParentId = parentDto.ParentId
            };

            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(duplicateDto));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_ParentNotFound()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var dto = new InsertionAssetDto { Name = "OrphanAsset", ParentId = Guid.NewGuid() };

            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(dto));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_NameIsNullOrEmpty()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var dtoEmpty = new InsertionAssetDto { Name = "" };
            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(dtoEmpty));

            var dtoNull = new InsertionAssetDto { Name = null };
            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(dtoNull));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_DuplicateUnderSameParent()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_mockLogger.Object, context);

            var parent = AssetTestData.RootAsset;
            await service.InsertAssetAsync(parent);

            var child1 = new InsertionAssetDto { Name = "Child", ParentId = null };
            await service.InsertAssetAsync(child1);

            var child2 = new InsertionAssetDto { Name = "Child", ParentId = null };
            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(child2));
        }
    }
}
