using System;

namespace CampusPulse.Domain.Entities
{
    public class AttendanceRecord
    {
        public int AttendanceRecordId { get; set; }

        public int AttendanceSessionId { get; set; }

        public int StudentId { get; set; }

        public DateTime ScannedAt { get; set; }

        public string Status { get; set; } = "";
    }
}