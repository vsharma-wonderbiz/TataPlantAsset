using Application.Interface;
using Application.Dtos;
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
        private readonly UserAuthDbContext _userDb;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(DBContext db, IHubContext<NotificationHub> hub, UserAuthDbContext userDb)
        {
            _db = db;
            _hub = hub;
            _userDb = userDb;
        }

        // -------------------------------
        // CREATE NOTIFICATION FOR ALL USERS
        // -------------------------------
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

            // Fetch all users from UserAuthService DB and convert Id to string
            var users = await _userDb.Users
                .Select(u => new { UserIdString = u.Id.ToString() })
                .ToListAsync();

            foreach (var user in users)
            {
                var recipient = new NotificationRecipient
                {
                    NotificationId = notification.Id,
                    UserId = user.UserIdString
                };

                _db.NotificationRecipients.Add(recipient);

                await _hub.Clients.User(user.UserIdString).SendAsync(
                    "ReceiveNotification",
                    new NotificationDto(
                        notification.Id,
                        notification.Title,
                        notification.Text,
                        notification.CreatedAt,
                        notification.ExpiresAt,
                        notification.Priority
                    )
                );
            }

            // Save recipients
            await _db.SaveChangesAsync();

            // ✅ RETURN ADDED
            return new NotificationDto(
                notification.Id,
                notification.Title,
                notification.Text,
                notification.CreatedAt,
                notification.ExpiresAt,
                notification.Priority
            );
        }


        // -------------------------------
        // MARK AS READ
        // -------------------------------
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

            await _hub.Clients.User(userId).SendAsync("NotificationMarkedRead", recipientId);

            return true;
        }

        // -------------------------------
        // ACKNOWLEDGE
        // -------------------------------
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

        // -------------------------------
        // GET ALL NOTIFICATIONS (ADMIN VIEW)
        // -------------------------------
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

        // -------------------------------
        // GET USER NOTIFICATIONS (PER USER)
        // -------------------------------
        public async Task<List<NotificationRecipientDto>> GetForUserAsync(string userId, bool unreadOnly)
        {
            var query = _db.NotificationRecipients
                .Include(r => r.Notification)
                .Where(r => r.UserId == userId);

            if (unreadOnly)
                query = query.Where(r => !r.IsRead);

            var list = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return list.Select(r => new NotificationRecipientDto
            {
                RecipientId = r.Id,
                NotificationId = r.NotificationId,
                Title = r.Notification.Title,
                Text = r.Notification.Text,
                IsRead = r.IsRead,
                IsAcknowledged = r.IsAcknowledged,
                CreatedAt = r.CreatedAt,
                ReadAt = r.ReadAt,
                AcknowledgedAt = r.AcknowledgedAt
            }).ToList();
        }



        // -------------------------------
        // MARK ALL AS READ
        // -------------------------------
        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            var recipients = await _db.NotificationRecipients
                .Where(r => r.UserId == userId && !r.IsRead)
                .ToListAsync();

            if (!recipients.Any())
                return false;

            foreach (var rec in recipients)
            {
                rec.IsRead = true;
                rec.ReadAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Notify user via SignalR for each notification
            foreach (var rec in recipients)
            {
                await _hub.Clients.User(userId).SendAsync("NotificationMarkedRead", rec.Id);
            }

            return true;
        }



    }
}
