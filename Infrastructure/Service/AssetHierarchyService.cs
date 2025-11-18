using Application.DTOs;
using Application.Interface;
using Domain.Entities;
using Infrastructure.DBs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Service
{
    public class AssetHierarchyService : IAssetHierarchyService
    {
        private readonly ILogger<AssetHierarchyService> _logger;
        private readonly DBContext _context;

        public AssetHierarchyService(ILogger<AssetHierarchyService> logger, DBContext context)
        {
            _logger = logger;
            _context = context;
        }

        public Asset GetAssetHierarchy()
        {
            try
            {
                var allAssets = _context.Assets
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted)
                    .ToList();

                // Create root if empty
                if (allAssets.Count == 0)
                {
                    try
                    {
                        var dto = new InsertionAssetDto { ParentId = null, Name = "Root Asset" };
                        InsertAsset(dto);

                        allAssets = _context.Assets.AsNoTracking().ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create root asset");
                        return null;
                    }
                }

                // Build tree
                var map = allAssets.ToDictionary(a => a.Id);
                foreach (var asset in allAssets)
                {
                    if (asset.ParentId.HasValue &&
                        map.ContainsKey(asset.ParentId.Value))
                    {
                        map[asset.ParentId.Value].Childrens.Add(asset);
                    }
                }

                return allAssets.FirstOrDefault(a => a.ParentId == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset hierarchy");
                return null;
            }
        }

        public async Task<List<AssetDto>> GetByParentIdAsync(int? parentId)
        {
            try
            {
                var items = await _context.Assets
                    .AsNoTracking()
                    .Where(a => a.ParentId == parentId && !a.IsDeleted)
                    .Select(a => new AssetDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        IsDeleted = a.IsDeleted
                    })
                    .ToListAsync();

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching children for ParentId {parentId}", parentId);
                return new List<AssetDto>();
            }
        }

        public bool InsertAsset(InsertionAssetDto dto)
        {
            try
            {
                // Validate parent
                if (dto.ParentId != null)
                {
                    var parent = _context.Assets.FirstOrDefault(a => a.Id == dto.ParentId);
                    if (parent == null)
                        return false;
                }

                var newAsset = new Asset
                {
                    Name = dto.Name,
                    ParentId = dto.ParentId
                };

                _context.Assets.Add(newAsset);
                _context.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
            {
                if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                {
                    _logger.LogWarning("Duplicate name detected for asset: {Name}", dto.Name);
                    return false;
                }

                _logger.LogError(ex, "SQL error inserting asset");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error inserting asset");
                return false;
            }
        }

        public async Task<(bool, string)> UpdateAsset(UpdateAssetDto dto)
        {
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == dto.Id);
                if (asset == null)
                    return (false, $"Asset with ID {dto.Id} not found.");

                // Validate duplicate name
                if (!string.IsNullOrWhiteSpace(dto.NewName) && dto.NewName != dto.OldName)
                {
                    bool nameExists = await _context.Assets
                        .AnyAsync(a => a.Name == dto.NewName && a.Id != dto.Id);

                    if (nameExists)
                        return (false, $"Asset name '{dto.NewName}' already exists.");
                }

                // Renaming only
                if (dto.OldParentId == dto.NewParentId && dto.OldName != dto.NewName)
                {
                    asset.Name = dto.NewName;
                    await _context.SaveChangesAsync();
                    return (true, $"Asset renamed to {asset.Name}");
                }

                // Moving to new parent
                if (dto.OldParentId != dto.NewParentId)
                {
                    var newParent = await _context.Assets.FirstOrDefaultAsync(a => a.Id == dto.NewParentId);
                    if (dto.NewParentId != 0 && newParent == null)
                        return (false, $"New parent with ID {dto.NewParentId} not found.");

                    // Prevent circular move
                    if (await IsDescendant(dto.Id, dto.NewParentId))
                        return (false, "Invalid move: cannot move an asset under its own descendant.");

                    asset.ParentId = dto.NewParentId == 0 ? null : dto.NewParentId;
                    await _context.SaveChangesAsync();
                    return (true, $"Asset moved to new parent ID {dto.NewParentId}");
                }

                return (false, "No changes detected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating asset");
                return (false, "Unexpected error occurred while updating asset.");
            }
        }


        private async Task<bool> IsDescendant(int assetId, int newParentId)
        {
            try
            {
                if (assetId == newParentId)
                    return true;

                var children = await _context.Assets
                    .Where(a => a.ParentId == assetId)
                    .Select(a => a.Id)
                    .ToListAsync();

                foreach (var child in children)
                {
                    if (await IsDescendant(child, newParentId))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking descendant relationship");
                return true; 
            }
        }

        public async Task<bool> DeleteAsset(int assetId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (assetId == 1)
                {
                    _logger.LogWarning("Attempted to delete root asset. Operation blocked.");
                    return false;
                }
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == assetId);
                if (asset == null)
                    return false;

                asset.IsDeleted = true;

                _context.Assets.Update(asset);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting asset");
                return false;
            }
        }
    }
}
