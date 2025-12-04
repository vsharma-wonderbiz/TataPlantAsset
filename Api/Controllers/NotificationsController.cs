using Application.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _svc;

        public NotificationsController(INotificationService svc) => _svc = svc;

        // -------------------------------
        // SEND Notification
        // -------------------------------
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] NotificationCreateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Request body is missing." });

            try
            {
                var created = await _svc.CreateForUsersAsync(req);

                return Ok(new
                {
                    message = "Notification sent successfully.",
                    data = created
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to send notification.", error = ex.Message });
            }
        }

        // -------------------------------
        // MARK AS READ
        // -------------------------------
        [HttpPost("read/{recipientId:guid}")]
        public async Task<IActionResult> MarkAsRead(Guid recipientId)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated." });

            try
            {
                var updated = await _svc.MarkAsReadAsync(recipientId, userId);

                if (!updated)
                    return NotFound(new { message = "Notification recipient not found or access denied." });

                return Ok(new { message = "Notification marked as read." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update notification.", error = ex.Message });
            }
        }

        // -------------------------------
        // ACKNOWLEDGE
        // -------------------------------
        [HttpPost("ack/{recipientId:guid}")]
        public async Task<IActionResult> Acknowledge(Guid recipientId)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated." });

            try
            {
                var updated = await _svc.AcknowledgeAsync(recipientId, userId);

                if (!updated)
                    return NotFound(new { message = "Notification recipient not found or access denied." });

                return Ok(new { message = "Notification acknowledged." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to acknowledge notification.", error = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _svc.GetAllNotificationsAsync();

                return Ok(new
                {
                    message = "Notifications fetched successfully.",
                    data = list
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch notifications.", error = ex.Message });
            }
        }


    }
}
