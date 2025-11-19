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
                var allAssets =await _context.Assets
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

                if (dto.ParentId.HasValue)
                {
                    Guid parentGuid = dto.ParentId.Value;
                    var parent = await _context.Assets
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.AssetId == parentGuid);

                    if (parent == null)
                        throw new Exception("Parent asset not found.");

                    level = parent.Level + 1;

                    if (level > 5)
                        throw new Exception("Asset cannot be added beyond Level 5.");
                }

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
                throw; // Pass DB exception to controller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error inserting asset.");
                throw; // This will be caught in controller
            }
        }





        public async Task<(bool, string)> UpdateAsset(UpdateAssetDto dto)
        {
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == dto.AssetId);
                if (asset == null)
                    return (false, $"Asset with ID {dto.AssetId} not found.");

                //Validate duplicate name
                if (!string.IsNullOrWhiteSpace(dto.NewName) && dto.NewName != dto.OldName)
                {
                    bool nameExists = await _context.Assets
                        .AnyAsync(a => a.Name == dto.NewName && a.AssetId != dto.AssetId);

                    if (nameExists)
                        return (false, $"Asset name '{dto.NewName}' already exists.");
                }

                //Renaming only
                if (dto.OldParentId == dto.NewParentId && dto.OldName != dto.NewName)
                {
                    asset.Name = dto.NewName;
                    await _context.SaveChangesAsync();
                    return (true, $"Asset renamed to {asset.Name}");
                }

                //Moving to new parent
                if (dto.OldParentId != dto.NewParentId)
                {
                    var newParent = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == dto.NewParentId);
                    if (dto.NewParentId != Guid.Empty && newParent == null)
                        return (false, $"New parent with ID {dto.NewParentId} not found.");

                    // Prevent circular move
                    if (await IsDescendant(dto.AssetId, dto.NewParentId))
                        return (false, "Invalid move: cannot move an asset under its own descendant.");

                    asset.ParentId = dto.NewParentId == Guid.Empty ? null : dto.NewParentId;
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
                var asset = await _context.Assets.Include(a => a.Childrens).FirstOrDefaultAsync(a => a.AssetId == assetId);
                
                //if (asset.ParentId == Guid.Empty || asset.ParentId==null)
                //{
                //    _logger.LogWarning("Attempted to delete root asset. Operation blocked.");
                //    return false;
                //}
                if (asset.Childrens!=null && asset.Childrens.Any())
                {
                    throw new Exception("Cannot delete this asset because it has child assets. Please delete or reassign its children first.");
                }
                //var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == assetId);
                if (asset == null)
                {
                    _logger.LogWarning("Unable To find the Asset");
                    return false;
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
    }
}
