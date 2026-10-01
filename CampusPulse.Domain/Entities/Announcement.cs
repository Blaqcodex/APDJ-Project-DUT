using System;

namespace CampusPulse.Domain.Entities
{
    public class Announcement
    {
        public int AnnouncementId { get; set; }

        public string Title { get; set; } = "";

        public string Message { get; set; } = "";

        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}