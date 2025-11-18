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
        public IActionResult GetAssetHierarchy()
        {
            var tree = service.GetAssetHierarchy();
            Console.WriteLine($"Data is {HttpContext.User}");
            return Ok(tree);
        }

        [HttpGet("[action]/{parentId?}")]
        public async Task<List<AssetDto>> GetByParentIdAsync(int? parentId)
        {
            var assets = await service.GetByParentIdAsync(parentId);
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
            bool isAdded =  service.InsertAsset(asset);

            if (isAdded)
            {
                return Ok("Asset Pushed Successfully");
            }
            return BadRequest("Parent not found or ID already exists.");
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
        public async Task<IActionResult> DeleteAsset(int id)
        {
            bool isRemoved = await service.DeleteAsset(id);
            if (isRemoved)
            {
                return Ok("Asset Deleted Successfully");
            }
            return BadRequest("Asset not found or Cannot delete Root.");
        }
    }
}
