namespace CampusPulse.Student.Services;

public record AttendanceCheckInResult(
    bool Success,
    string Message);

public interface IStudentAttendanceApiService
{
    Task<AttendanceCheckInResult> CheckInAsync(string qrToken);
}