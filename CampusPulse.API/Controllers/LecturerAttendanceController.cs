using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

public sealed class StartAttendanceRequest
{
    public int TimetableId { get; set; }
    public int DurationMinutes { get; set; } = 60;
}

[ApiController]
[Route("api/lecturer/attendance")]
[Authorize(Roles = "Lecturer")]
public class LecturerAttendanceController : ControllerBase
{
    private readonly CampusPulseDbContext db;

    public LecturerAttendanceController(CampusPulseDbContext db)
    {
        this.db = db;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(
        [FromBody] StartAttendanceRequest request)
    {
        if (request.DurationMinutes < 5 ||
            request.DurationMinutes > 180)
        {
            return BadRequest(new
            {
                message = "Duration must be between 5 and 180 minutes."
            });
        }

        var lecturer = await GetLecturerAsync();

        if (lecturer == null)
            return Unauthorized();

        bool ownsTimetable = await db.Timetables.AnyAsync(t =>
            t.TimetableId == request.TimetableId &&
            t.LecturerId == lecturer.LecturerId);

        if (!ownsTimetable)
            return Forbid();

        DateTime now = DateTime.UtcNow;

        bool alreadyActive = await db.AttendanceSessions.AnyAsync(s =>
            s.TimetableId == request.TimetableId &&
            s.Status == "Active" &&
            s.ExpiresAt > now);

        if (alreadyActive)
        {
            return Conflict(new
            {
                message = "An active attendance session already exists."
            });
        }

        string token = CreateToken();

        var session = new AttendanceSession
        {
            TimetableId = request.TimetableId,
            SessionDate = now.Date,
            StartedAt = now,
            ExpiresAt = now.AddMinutes(request.DurationMinutes),
            QrToken = HashToken(token),
            QrTokenExpiresAt = now.AddSeconds(20),
            Status = "Active"
        };

        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync();

        return Ok(new
        {
            sessionId = session.AttendanceSessionId,
            qrToken = token,
            qrExpiresAt = session.QrTokenExpiresAt,
            sessionExpiresAt = session.ExpiresAt
        });
    }

    [HttpPost("{sessionId:int}/rotate")]
    public async Task<IActionResult> Rotate(int sessionId)
    {
        var lecturer = await GetLecturerAsync();

        if (lecturer == null)
            return Unauthorized();

        var session = await (
            from s in db.AttendanceSessions
            join t in db.Timetables
                on s.TimetableId equals t.TimetableId
            where s.AttendanceSessionId == sessionId &&
                  t.LecturerId == lecturer.LecturerId
            select s
        ).FirstOrDefaultAsync();

        if (session == null)
            return NotFound();

        DateTime now = DateTime.UtcNow;

        if (session.Status != "Active" ||
            session.ExpiresAt <= now)
        {
            return BadRequest(new
            {
                message = "Attendance session has ended."
            });
        }

        string token = CreateToken();

        session.QrToken = HashToken(token);
        session.QrTokenExpiresAt =
            now.AddSeconds(20) < session.ExpiresAt
                ? now.AddSeconds(20)
                : session.ExpiresAt;

        await db.SaveChangesAsync();

        return Ok(new
        {
            sessionId,
            qrToken = token,
            qrExpiresAt = session.QrTokenExpiresAt
        });
    }

    [HttpPost("{sessionId:int}/end")]
    public async Task<IActionResult> End(int sessionId)
    {
        var lecturer = await GetLecturerAsync();

        if (lecturer == null)
            return Unauthorized();

        var session = await (
            from s in db.AttendanceSessions
            join t in db.Timetables
                on s.TimetableId equals t.TimetableId
            where s.AttendanceSessionId == sessionId &&
                  t.LecturerId == lecturer.LecturerId
            select s
        ).FirstOrDefaultAsync();

        if (session == null)
            return NotFound();

        session.Status = "Closed";
        session.QrToken = "";
        session.QrTokenExpiresAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new { message = "Attendance session closed." });
    }

    private async Task<Lecturer?> GetLecturerAsync()
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int userId))
        {
            return null;
        }

        return await db.Lecturers.FirstOrDefaultAsync(
            l => l.UserId == userId);
    }

    private static string CreateToken()
    {
        return Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}