namespace CampusPulse.Student.Services;

public interface IStudentNotificationsApiService
{
    Task<IReadOnlyList<StudentNotification>> GetNotificationsAsync();

    Task<int> GetUnreadCountAsync();

    Task MarkAsReadAsync(int notificationId);
}