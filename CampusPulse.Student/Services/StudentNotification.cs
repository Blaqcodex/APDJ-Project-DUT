namespace CampusPulse.Student.Services;

public class StudentNotification
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = "";

    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}