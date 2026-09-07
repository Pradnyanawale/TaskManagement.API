using TaskManagement.API.Models;

namespace TaskManagement.API.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string message);

        Task<List<Notification>> GetUserNotificationsAsync(int userId);

        Task MarkAsReadAsync(int notificationId, int userId);
    }
}