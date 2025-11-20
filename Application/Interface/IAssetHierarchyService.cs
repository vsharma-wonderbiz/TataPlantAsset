using Application.DTOs;
using Domain.Entities;

namespace Application.Interface
{
    public interface IAssetHierarchyService
    {
        Task<List<Asset>> GetAssetHierarchy();
        Task<List<AssetDto>> GetByParentIdAsync(Guid? parentId);
        Task<bool> InsertAssetAsync(InsertionAssetDto dto);

        Task<(bool, string)> UpdateAsset(UpdateAssetDto currAsset);

        Task<bool> DeleteAsset(Guid AssetId);
        Task<List<AssetDto>> SearchAssetsAsync(string? searchTerm);

    }
}