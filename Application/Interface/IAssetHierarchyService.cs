using Application.DTOs;
using Domain.Entities;

namespace Application.Interface
{
    public interface IAssetHierarchyService
    {
        Task<List<Asset>> GetAssetHierarchy(string? searchTerm = null);
        Task<List<AssetDto>> GetByParentIdAsync(Guid? parentId, string? searchTerm);
        Task<bool> InsertAssetAsync(InsertionAssetDto dto);

        Task<(bool, string)> UpdateAsset(UpdateAssetDto currAsset);

        Task<bool> DeleteAsset(Guid AssetId);

    }
}