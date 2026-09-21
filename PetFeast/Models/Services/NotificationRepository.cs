using Microsoft.AspNetCore.Mvc;
using PetFeast.Data;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Notifications;
using Microsoft.EntityFrameworkCore;

namespace PetFeast.Models.Services
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly PetFeastDBContext _context;

        public NotificationRepository(
            PetFeastDBContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetByUserIdAsync(
            string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(
            string userId)
        {
            return await _context.Notifications
                .CountAsync(n =>
                    n.UserId == userId &&
                    !n.IsRead);
        }

        public async Task MarkAsReadAsync(
            int notificationId,
            string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == notificationId &&
                    n.UserId == userId);

            if (notification == null)
            {
                return;
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();
        }
    }
}
