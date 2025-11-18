using Application.DTOs;
using Domain.Entities;

namespace Application.Interface
{
    public interface IAssetHierarchyService
    {
        Asset GetAssetHierarchy();
        Task<List<AssetDto>> GetByParentIdAsync(int? parentId);
        bool InsertAsset(InsertionAssetDto currAsset);

        Task<(bool, string)> UpdateAsset(UpdateAssetDto currAsset);

        Task<bool> DeleteAsset(int AssetId);
    }
}