using System.Threading.Tasks;
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

        public async Task<List<Asset>> GetAssetHierarchy()
        {
            try
            {
                var allAssets = await _context.Assets
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted)
                    .ToListAsync();



                var map = allAssets.ToDictionary(a => a.AssetId);
                foreach (var asset in allAssets)
                {
                    if (asset.ParentId.HasValue &&
                        map.ContainsKey(asset.ParentId.Value))
                    {
                        map[asset.ParentId.Value].Childrens.Add(asset);
                    }
                }

                var roots = allAssets.Where(a => a.ParentId == null || a.ParentId == Guid.Empty).ToList();

                return roots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset hierarchy");
                return null;
            }
        }

        public async Task<List<AssetDto>> GetByParentIdAsync(Guid? parentId)
        {
            try
            {
                var items = await _context.Assets
                    .AsNoTracking()
                    .Where(a => a.ParentId == parentId && !a.IsDeleted)
                    .Select(a => new AssetDto
                    {
                        Id = a.AssetId,
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

        public async Task<bool> InsertAssetAsync(InsertionAssetDto dto)
        {
            try
            {
                int level = 1;

                //Handle parent level logic
                if (dto.ParentId.HasValue)
                {
                    var parent = await _context.Assets
                            .AsNoTracking()
                            .FirstOrDefaultAsync(a => a.AssetId == dto.ParentId.Value && !a.IsDeleted);

                    if (parent == null)
                        throw new Exception("Parent asset not found.");

                    level = parent.Level + 1;

                    if (level > 5)
                        throw new Exception("Asset cannot be added beyond Level 5.");
                }

                //Check existing name (even if deleted)
                var existing = await _context.Assets
                    .FirstOrDefaultAsync(a => a.Name == dto.Name);

                if (existing != null)
                {
                    if (existing.IsDeleted)
                    {
                        // Restore the deleted asset
                        existing.IsDeleted = false;
                        existing.ParentId = dto.ParentId;
                        existing.Level = level;

                        _context.Assets.Update(existing);
                        await _context.SaveChangesAsync();

                        return true;
                    }

                    throw new Exception("Asset name already exists.");
                }

                //Insert new asset
                var newAsset = new Asset
                {
                    Name = dto.Name,
                    ParentId = dto.ParentId,
                    Level = level
                };

                await _context.Assets.AddAsync(newAsset);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error inserting asset.");
                throw new Exception("Database error occurred while inserting asset. Details: " + ex.InnerException?.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error inserting asset.");
                throw;
            }
        }







        //public async Task<(bool, string)> UpdateAsset(UpdateAssetDto dto)
        //{
        //    try
        //    {
        //        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == dto.AssetId);
        //        if (asset == null)
        //            return (false, $"Asset with ID {dto.AssetId} not found.");

        //        //Validate duplicate name
        //        if (!string.IsNullOrWhiteSpace(dto.NewName) && dto.NewName != dto.OldName)
        //        {
        //            bool nameExists = await _context.Assets
        //                .AnyAsync(a => a.Name == dto.NewName && a.AssetId != dto.AssetId);

        //            if (nameExists)
        //                return (false, $"Asset name '{dto.NewName}' already exists.");
        //        }

        //        //Renaming only
        //        if (dto.OldParentId == dto.NewParentId && dto.OldName != dto.NewName)
        //        {
        //            asset.Name = dto.NewName;
        //            await _context.SaveChangesAsync();
        //            return (true, $"Asset renamed to {asset.Name}");
        //        }

        //        //Moving to new parent
        //        if (dto.OldParentId != dto.NewParentId)
        //        {
        //            var newParent = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == dto.NewParentId);
        //            if (dto.NewParentId != Guid.Empty && newParent == null)
        //                return (false, $"New parent with ID {dto.NewParentId} not found.");

        //            // Prevent circular move
        //            if (await IsDescendant(dto.AssetId, dto.NewParentId))
        //                return (false, "Invalid move: cannot move an asset under its own descendant.");

        //            asset.ParentId = dto.NewParentId == Guid.Empty ? null : dto.NewParentId;
        //            await _context.SaveChangesAsync();
        //            return (true, $"Asset moved to new parent ID {dto.NewParentId}");
        //        }

        //        return (false, "No changes detected.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating asset");
        //        return (false, "Unexpected error occurred while updating asset.");
        //    }
        //}

        public async Task<(bool Success, string Message)> UpdateAssetName(UpdateAssetDto dto)
        {
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == dto.AssetId);
                if (asset == null)
                    return (false, $"Asset with ID {dto.AssetId} not found.");

                // Check if name already exists, even if deleted
                var existing = await _context.Assets
                    .FirstOrDefaultAsync(a => a.Name == dto.NewName && a.AssetId != dto.AssetId);

                if (existing != null)
                {
                    if (existing.IsDeleted)
                    {
                        // Restore the soft-deleted asset
                        existing.IsDeleted = false;
                        existing.ParentId = asset.ParentId;  // Keep it under the same parent as renamed asset
                        existing.Level = asset.Level;

                        _context.Assets.Update(existing);
                        await _context.SaveChangesAsync();

                        return (true, $"Soft-deleted asset '{existing.Name}' has been restored.");
                    }

                    return (false, $"Asset name '{dto.NewName}' already exists.");
                }

                // Update the asset name
                asset.Name = dto.NewName;
                await _context.SaveChangesAsync();

                return (true, $"Asset renamed to {asset.Name}");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating asset.");
                return (false, "Database error occurred while updating asset.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating asset.");
                return (false, "Unexpected error occurred while updating asset.");
            }
        }


        private async Task<bool> IsDescendant(Guid assetId, Guid newParentId)
        {
            try
            {
                if (assetId == newParentId)
                    return true;

                var children = await _context.Assets
                    .Where(a => a.ParentId == assetId)
                    .Select(a => a.AssetId)
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

        public async Task<bool> DeleteAsset(Guid assetId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                bool ConnectedToDevice = await _context.MappingTable.AnyAsync(a => a.AssetId == assetId);

                if (ConnectedToDevice)
                    throw new Exception("Unassign the Device to Delete the asset");

                var asset = await _context.Assets
                    .Include(a => a.Childrens.Where(c => !c.IsDeleted))//see only the active children
                    .FirstOrDefaultAsync(a => a.AssetId == assetId);

                if (asset == null)
                {
                    _logger.LogWarning("Unable to find the Asset");
                    return false;
                }

                //see only active children
                if (asset.Childrens.Any())
                {
                    throw new Exception("Cannot delete this asset because it has child assets. Please delete or reassign its children first.");
                }

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
                throw;
            }
        }


        public async Task<List<AssetDto>> SearchAssetsAsync(string? searchTerm)
        {
            try
            {
                // Start with all non-deleted assets
                var query = _context.Assets
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted);

                // Apply search if searchTerm is not null or empty
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    string lowerTerm = searchTerm.ToLower();
                    query = query.Where(a => a.Name.ToLower().Contains(lowerTerm));
                }

                var result = await query
                    .Select(a => new AssetDto
                    {
                        Id = a.AssetId,
                        Name = a.Name,
                        IsDeleted = a.IsDeleted
                    })
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching assets with term: {searchTerm}", searchTerm);
                return new List<AssetDto>();
            }
        }

        public async Task<List<AssetDto>> GetDeletedAssetsAsync()
        {
            try
            {
                var deletedAssets = await _context.Assets
                    .AsNoTracking()
                    .Where(a => a.IsDeleted)
                    .Select(a => new AssetDto
                    {
                        Id = a.AssetId,
                        Name = a.Name,
                        IsDeleted = a.IsDeleted
                    })
                    .ToListAsync();

                return deletedAssets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching deleted assets");
                return new List<AssetDto>();
            }
        }


        public async Task<bool> RestoreAssetAsync(Guid assetId)
        {
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == assetId);
            if (asset == null)
                throw new KeyNotFoundException("Asset not found.");

            if (!asset.IsDeleted)
                throw new InvalidOperationException("Asset is not deleted.");

            asset.IsDeleted = false;
            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();
            return true;
        }



    }
}
