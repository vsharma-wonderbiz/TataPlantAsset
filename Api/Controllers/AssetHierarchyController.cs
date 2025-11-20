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
        public async Task<IActionResult> GetAssetHierarchy()
        {
            
            var tree = await service.GetAssetHierarchy();
            Console.WriteLine($"Data is {HttpContext.User}");
            return Ok(tree);
        }

        [HttpGet("[action]/{parentId?}")]
        public async Task<List<AssetDto>> GetByParentIdAsync(Guid? parentId)
        {
            var assets = await service.GetByParentIdAsync(parentId);
            Console.WriteLine("Recieved ........");
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

        [HttpGet("Search")]
        public async Task<IActionResult> SearchAssets([FromQuery] string? term)
        {
            var results = await service.SearchAssetsAsync(term);
            return Ok(results);
        }

    }
}
