using Microsoft.AspNetCore.Mvc;
using InfluxDB.Client;
using Infrastructure.DBs;
using Application.Interface;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelemetryTestController : ControllerBase
    {
        private readonly IInfluxDbConnectionService _connectionService;

        public TelemetryTestController(IInfluxDbConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        [HttpGet("health")]
        public IActionResult TestConnection()
        {
            try
            {
                var client = _connectionService.GetClient();

                // Direct try to ping server (minimal check)
                var health = client.HealthAsync().GetAwaiter().GetResult();

                // Just return status string, no enum check
                return Ok(new
                {
                    status = "Connected",
                    message = $"InfluxDB server version: {health.Version}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Failed",
                    message = ex.Message
                });
            }
        }
    }
}
