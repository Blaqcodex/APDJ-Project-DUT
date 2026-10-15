using System;

namespace CampusPulse.Domain.Entities
{
    public class AttendanceSession
    {
        public int AttendanceSessionId { get; set; }

        public int TimetableId { get; set; }

        public DateTime SessionDate { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        // SHA-256 hash of the current short-lived QR token.
        public string QrToken { get; set; } = "";

        public DateTime QrTokenExpiresAt { get; set; }

        public string Status { get; set; } = "";
    }
}