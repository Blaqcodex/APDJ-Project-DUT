namespace CampusPulse.Student.Services;

public interface IStudentTimetableApiService
{
    Task<IReadOnlyList<StudentTimetableEntry>> GetTimetableAsync();
}