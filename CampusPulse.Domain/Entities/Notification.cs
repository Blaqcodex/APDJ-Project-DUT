using System;

namespace CampusPulse.Domain.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; } = "";

        public string Message { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}