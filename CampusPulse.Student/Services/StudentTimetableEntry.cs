namespace CampusPulse.Student.Services;

public class StudentTimetableEntry
{
    public int TimetableId { get; set; }

    public string ModuleCode { get; set; } = "";

    public string ModuleName { get; set; } = "";

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string VenueName { get; set; } = "";

    public string Campus { get; set; } = "";

    public string LecturerName { get; set; } = "";
}