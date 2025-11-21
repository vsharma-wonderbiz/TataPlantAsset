using System.Threading.Tasks;
using Application.DTOs;
using Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetHierarchyController : ControllerBase
    {
        private readonly IAssetHierarchyService service;
        public AssetHierarchyController(IAssetHierarchyService service)
        {
            this.service = service;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAssetHierarchy([FromQuery] string? term = null)
        {
            try
            {
                // Get full hierarchy
                var allAssets = await service.GetAssetHierarchy();

                // If keyword search is provided, filter roots only
                if (!string.IsNullOrWhiteSpace(term))
                {
                    string lowerTerm = term.ToLower();
                    allAssets = allAssets
                        .Where(a => a.Name.ToLower().Contains(lowerTerm))
                        .ToList();
                }

                return Ok(allAssets);
            }
            catch (Exception ex)
            {
                // log if needed
                return StatusCode(500, "Error retrieving asset hierarchy");
            }
        }

        [HttpGet("[action]/{parentId?}")]
        public async Task<List<AssetDto>> GetByParentIdAsync(Guid? parentId, [FromQuery] string? term = null)
        {
            var assets = await service.GetByParentIdAsync(parentId, term);
            Console.WriteLine("Recieved ........ ");

            foreach (var asset in assets)
            {
                Console.WriteLine($"Asset ID: {asset.Id}, Name: {asset.Name}, IsDeleted: {asset.IsDeleted}");
            }
            return assets;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> InsertAsset([FromBody] InsertionAssetDto asset)
        {
            try
            {
                bool isAdded = await service.InsertAssetAsync(asset);

                if (isAdded)
                    return Ok("Asset added successfully.");

               
                return BadRequest("Unable to add asset.");
            }
            catch (Exception ex)
            {
               //to catch the error thrown from service layer 
                return BadRequest(ex.Message);
            }
        }


        [HttpPut("[action]")]
        public async Task<IActionResult> UpdateAsset([FromBody] UpdateAssetDto asset)
        {
            var (isAdded, message) = await service.UpdateAsset(asset);

            if (isAdded)
            {
                return Ok(message);
            }
            return BadRequest(message);
        }

            [HttpDelete("[action]/{id}")]
            public async Task<IActionResult> DeleteAsset(Guid id)
            {
                try
                {

                    bool isRemoved = await service.DeleteAsset(id);
                    if (isRemoved)
                    {
                        return Ok("Asset Deleted Successfully");
                    }
                    else
                    {
                        return BadRequest("unable to delete asste ");
                    }
                }catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
               
           
            }
        // GET /api/AssetHierarchy/deleted
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedAssets()
        {
            try
            {
                var list = await service.GetDeletedAssetsAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // GET /api/AssetHierarchy/deleted/{id}
        [HttpGet("deleted/{id:guid}")]
        public async Task<IActionResult> GetDeletedAsset(Guid id)
        {
            try
            {
                var asset = await service.GetDeletedAssetAsync(id);
                if (asset == null)
                    return NotFound("Deleted asset not found.");

                return Ok(asset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // POST /api/AssetHierarchy/{id}/restore
        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> RestoreAsset(Guid id)
        {
            try
            {
                await service.RestoreAssetAsync(id);
                return Ok("Asset restored successfully.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Asset not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }


    }
}
