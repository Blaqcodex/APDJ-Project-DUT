using System;

namespace CampusPulse.Domain.Entities
{
    public class Timetable
    {
        public int TimetableId { get; set; }

        public int ModuleId { get; set; }

        public int LecturerId { get; set; }

        public int VenueId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }
    }
}