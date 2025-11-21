using Microsoft.AspNetCore.Mvc;

using MappingService.DTOs;
using MappingService.Domain.Entities;
using Application.Interface;
using Application.DTOs;

namespace MappingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MappingController : ControllerBase
    {
        private readonly IMappingService _mappingService;

        public MappingController(IMappingService mappingService)
        {
            _mappingService = mappingService;
        }

        // POST api/mapping
        [HttpPost]
        public async Task<IActionResult> CreateMapping([FromBody] CreateMappingDto dto)
        {
            if (dto.AssetId == Guid.Empty || dto.DeviceId == Guid.Empty || dto.DevicePortId == Guid.Empty)
            {
                return BadRequest("AssetId, DeviceId, and DevicePortId are required.");
            }

            try
            {
                var mappings = await _mappingService.CreateMapping(dto);

                return Ok(new
                {
                    Message = "Mapping created successfully",
                    Data = mappings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // GET api/mapping
        [HttpGet]
        public async Task<IActionResult> GetMappings()
        {
            try
            {
                var mappings = await _mappingService.GetMappings();
                return Ok(mappings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}
