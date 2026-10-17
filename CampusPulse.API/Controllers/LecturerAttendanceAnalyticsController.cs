using System.Security.Claims;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

[ApiController]
[Route("api/lecturer/attendance-analytics")]
[Authorize(Roles = "Lecturer")]
public sealed class LecturerAttendanceAnalyticsController : ControllerBase
{
    private readonly CampusPulseDbContext _db;

    public LecturerAttendanceAnalyticsController(CampusPulseDbContext db)
    {
        _db = db;
    }

    [HttpGet("modules/{moduleId:int}")]
    public async Task<IActionResult> GetModuleAnalytics(int moduleId)
    {
        if (moduleId <= 0)
        {
            return BadRequest(new { message = "Invalid module ID." });
        }

        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int userId))
        {
            return Unauthorized();
        }

        var lecturer = await _db.Lecturers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.UserId == userId);

        if (lecturer == null)
        {
            return Unauthorized();
        }

        bool assigned = await _db.LecturerModules
            .AsNoTracking()
            .AnyAsync(lm =>
                lm.LecturerId == lecturer.LecturerId &&
                lm.ModuleId == moduleId);

        if (!assigned)
        {
            return Forbid();
        }

        var module = await _db.Modules
            .AsNoTracking()
            .Where(m => m.ModuleId == moduleId)
            .Select(m => new
            {
                m.ModuleId,
                m.ModuleCode,
                m.ModuleName
            })
            .FirstOrDefaultAsync();

        if (module == null)
        {
            return NotFound(new { message = "Module not found." });
        }

        var students = await (
            from enrollment in _db.StudentModules.AsNoTracking()
            join student in _db.Students.AsNoTracking()
                on enrollment.StudentId equals student.StudentId
            join user in _db.Users.AsNoTracking()
                on student.UserId equals user.UserId
            where enrollment.ModuleId == moduleId
            orderby student.StudentNumber
            select new
            {
                student.StudentId,
                student.StudentNumber,
                user.FirstName,
                user.LastName
            }
        ).Distinct().ToListAsync();

        DateTime now = DateTime.UtcNow;

        var sessions = await (
            from session in _db.AttendanceSessions.AsNoTracking()
            join timetable in _db.Timetables.AsNoTracking()
                on session.TimetableId equals timetable.TimetableId
            where timetable.ModuleId == moduleId &&
                  timetable.LecturerId == lecturer.LecturerId
            select new
            {
                session.AttendanceSessionId,
                session.SessionDate,
                session.ExpiresAt,
                session.Status
            }
        ).ToListAsync();

        var completedSessions = sessions
            .Where(s =>
                string.Equals(
                    s.Status,
                    "Closed",
                    StringComparison.OrdinalIgnoreCase) ||
                s.ExpiresAt <= now)
            .ToList();

        var completedIds = completedSessions
            .Select(s => s.AttendanceSessionId)
            .ToList();

        var presentRecords = await _db.AttendanceRecords
            .AsNoTracking()
            .Where(record =>
                completedIds.Contains(record.AttendanceSessionId) &&
                record.Status == "Present")
            .Select(record => new
            {
                record.AttendanceSessionId,
                record.StudentId
            })
            .ToListAsync();

        var presentLookup = presentRecords
            .Select(r => (r.AttendanceSessionId, r.StudentId))
            .ToHashSet();

        int completedCount = completedSessions.Count;

        var studentResults = students
            .Select(student =>
            {
                int present = completedSessions.Count(session =>
                    presentLookup.Contains((
                        session.AttendanceSessionId,
                        student.StudentId)));

                int absent = completedCount - present;

                double? percentage = completedCount == 0
                    ? null
                    : Math.Round(
                        present * 100.0 / completedCount,
                        1);

                return new
                {
                    student.StudentId,
                    student.StudentNumber,
                    student.FirstName,
                    student.LastName,
                    Present = present,
                    Absent = absent,
                    CompletedSessions = completedCount,
                    AttendancePercentage = percentage
                };
            })
            .ToList();

        int totalPresent = studentResults.Sum(s => s.Present);
        int totalPossible = students.Count * completedCount;

        double? overallPercentage = totalPossible == 0
            ? null
            : Math.Round(
                totalPresent * 100.0 / totalPossible,
                1);

        return Ok(new
        {
            module.ModuleId,
            module.ModuleCode,
            module.ModuleName,
            EnrollmentBasis = "Current module enrollments",
            EnrolledStudents = students.Count,
            CompletedSessions = completedCount,
            PendingSessions = sessions.Count - completedCount,
            TotalPresent = totalPresent,
            TotalAbsent = totalPossible - totalPresent,
            AttendancePercentage = overallPercentage,
            Students = studentResults
        });
    }
}