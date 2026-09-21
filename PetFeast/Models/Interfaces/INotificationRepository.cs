using PetFeast.Models.Notifications;

namespace PetFeast.Models.Interfaces
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);

        Task<List<Notification>> GetByUserIdAsync(string userId);

        Task<int> GetUnreadCountAsync(string userId);

        Task MarkAsReadAsync(int notificationId,string userId);
    }
}
