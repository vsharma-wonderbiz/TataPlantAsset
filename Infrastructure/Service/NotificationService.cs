using Application.Interface;
using Domain.Entities;
using Infrastructure.DBs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Service
{
    public class NotificationService : INotificationService
    {
        private readonly DBContext _db;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(DBContext db, IHubContext<NotificationHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        public async Task<NotificationDto> CreateForUsersAsync(NotificationCreateRequest req)
        {
            var now = DateTime.UtcNow;
            var notification = new Notification
            {
                Title = req.Title,
                Text = req.Text,
                CreatedAt = now,
                ExpiresAt = req.ExpiresAt ?? now.AddDays(30),
                Priority = req.Priority
            };

            _db.Notifications.Add(notification);

            await _db.SaveChangesAsync();

            var dto = new NotificationDto(
                notification.Id,
                notification.Title,
                notification.Text,
                notification.CreatedAt,
                notification.ExpiresAt,
                notification.Priority
            );

            await _hub.Clients.All.SendAsync("ReceiveNotification", dto);


            return dto;
        }

        /// <summary>
        /// Marks the recipient as read. Returns true if updated; false if not found / not owned by user.
        /// Throws on unexpected DB errors.
        /// </summary>
        public async Task<bool> MarkAsReadAsync(Guid recipientId, string userId)
        {
            var rec = await _db.NotificationRecipients
                .FirstOrDefaultAsync(r => r.Id == recipientId && r.UserId == userId);

            if (rec == null) return false;

            if (!rec.IsRead)
            {
                rec.IsRead = true;
                rec.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // notify user's connected clients if desired
            await _hub.Clients.User(userId).SendAsync("NotificationMarkedRead", recipientId);

            return true;
        }

        /// <summary>
        /// Marks the recipient as acknowledged. Returns true if updated; false if not found / not owned by user.
        /// Throws on unexpected DB errors.
        /// </summary>
        public async Task<bool> AcknowledgeAsync(Guid recipientId, string userId)
        {
            var rec = await _db.NotificationRecipients
                .FirstOrDefaultAsync(r => r.Id == recipientId && r.UserId == userId);

            if (rec == null) return false;

            if (!rec.IsAcknowledged)
            {
                rec.IsAcknowledged = true;
                rec.AcknowledgedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            await _hub.Clients.User(userId).SendAsync("NotificationAcknowledged", recipientId);

            return true;
        }

        public async Task<List<NotificationDto>> GetAllNotificationsAsync()
        {
            var list = await _db.Notifications
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto(
                    n.Id,
                    n.Title,
                    n.Text,
                    n.CreatedAt,
                    n.ExpiresAt,
                    n.Priority
                ))
                .ToListAsync();

            return list;
        }

    }
}
