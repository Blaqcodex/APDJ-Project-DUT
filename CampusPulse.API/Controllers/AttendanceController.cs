using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

public sealed class AttendanceCheckInRequest
{
    public string? QrToken { get; set; }
}

[ApiController]
[Route("api/attendance")]
[Authorize(Roles = "Student")]
public class AttendanceController : ControllerBase
{
    private readonly CampusPulseDbContext db;

    public AttendanceController(CampusPulseDbContext db)
    {
        this.db = db;
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(
        [FromBody] AttendanceCheckInRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.QrToken) ||
            request.QrToken.Length > 512)
        {
            return BadRequest(new { message = "Invalid QR code." });
        }

        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int userId))
        {
            return Unauthorized();
        }

        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
        {
            return NotFound(new { message = "Student account not found." });
        }

        string suppliedToken = request.QrToken.Trim();

        // The database stores a SHA-256 hash, not the raw QR secret.
        string tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(suppliedToken)));

        var session = await db.AttendanceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.QrToken == tokenHash);

        if (session == null)
        {
            return BadRequest(new { message = "QR code is invalid." });
        }

        DateTime now = DateTime.UtcNow;

        if (!string.Equals(
                session.Status, "Active",
                StringComparison.OrdinalIgnoreCase) ||
            session.StartedAt > now ||
            session.ExpiresAt <= now ||
            session.QrTokenExpiresAt <= now)
        {
            return BadRequest(new
            {
                message = "Attendance session is not active or has expired."
            });
        }

        var timetable = await db.Timetables
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TimetableId == session.TimetableId);

        if (timetable == null)
        {
            return BadRequest(new { message = "Lecture not found." });
        }

        bool enrolled = await db.StudentModules
            .AsNoTracking()
            .AnyAsync(sm =>
                sm.StudentId == student.StudentId &&
                sm.ModuleId == timetable.ModuleId);

        if (!enrolled)
        {
            return StatusCode(403, new
            {
                message = "You are not enrolled in this module."
            });
        }

        bool alreadyRecorded = await db.AttendanceRecords
            .AnyAsync(a =>
                a.AttendanceSessionId == session.AttendanceSessionId &&
                a.StudentId == student.StudentId);

        if (alreadyRecorded)
        {
            return Conflict(new
            {
                message = "Your attendance has already been recorded."
            });
        }

        db.AttendanceRecords.Add(new()
        {
            AttendanceSessionId = session.AttendanceSessionId,
            StudentId = student.StudentId,
            ScannedAt = now,
            Status = "Present"
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The database unique index prevents simultaneous duplicates.
            // Distinguish duplicate submissions from other DB failures.
            bool duplicate = await db.AttendanceRecords
                .AsNoTracking()
                .AnyAsync(a =>
                    a.AttendanceSessionId == session.AttendanceSessionId &&
                    a.StudentId == student.StudentId);

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "Your attendance has already been recorded."
                });
            }

            throw;
        }

        return Ok(new
        {
            message = "Attendance recorded successfully.",
            sessionId = session.AttendanceSessionId,
            recordedAt = now
        });
    }
}