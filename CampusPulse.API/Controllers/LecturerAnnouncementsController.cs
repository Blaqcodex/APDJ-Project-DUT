using System.Security.Claims;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

public sealed class PublishAnnouncementRequest
{
    public int ModuleId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
}

[ApiController]
[Route("api/lecturer/announcements")]
[Authorize(Roles = "Lecturer")]
public sealed class LecturerAnnouncementsController : ControllerBase
{
    private readonly CampusPulseDbContext db;

    public LecturerAnnouncementsController(CampusPulseDbContext db)
    {
        this.db = db;
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetAssignedModules()
    {
        var lecturer = await GetLecturerAsync();

        if (lecturer == null)
            return Unauthorized();

        var moduleIds = db.Timetables
            .Where(t => t.LecturerId == lecturer.LecturerId)
            .Select(t => t.ModuleId)
            .Distinct();

        var modules = await db.Modules
            .AsNoTracking()
            .Where(m => moduleIds.Contains(m.ModuleId))
            .OrderBy(m => m.ModuleCode)
            .Select(m => new
            {
                moduleId = m.ModuleId,
                moduleCode = m.ModuleCode,
                moduleName = m.ModuleName
            })
            .ToListAsync();

        return Ok(modules);
    }

    [HttpPost]
    public async Task<IActionResult> Publish(
        [FromBody] PublishAnnouncementRequest request)
    {
        var lecturer = await GetLecturerAsync();

        if (lecturer == null)
            return Unauthorized();

        if (request.ModuleId <= 0 ||
            string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                message = "Module, title and message are required."
            });
        }

        string title = request.Title.Trim();
        string message = request.Message.Trim();

        if (title.Length > 150 || message.Length > 5000)
        {
            return BadRequest(new
            {
                message = "Title must be at most 150 characters and message at most 5000 characters."
            });
        }

        bool ownsModule = await db.Timetables.AnyAsync(t =>
            t.LecturerId == lecturer.LecturerId &&
            t.ModuleId == request.ModuleId);

        if (!ownsModule)
            return Forbid();

        var module = await db.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModuleId == request.ModuleId);

        if (module == null)
            return NotFound();

        var recipientIds = await (
            from enrollment in db.StudentModules
            join student in db.Students
                on enrollment.StudentId equals student.StudentId
            where enrollment.ModuleId == request.ModuleId
            select student.UserId
        )
        .Distinct()
        .ToListAsync();

        DateTime now = DateTime.UtcNow;

        var announcement = new Announcement
        {
            ModuleId = request.ModuleId,
            Title = title,
            Message = message,
            CreatedByUserId = lecturer.UserId,
            CreatedAt = now
        };

        db.Announcements.Add(announcement);

        string notificationTitle = $"{module.ModuleCode}: {title}";

        foreach (int userId in recipientIds)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = notificationTitle,
                Message = message,
                CreatedAt = now,
                IsRead = false
            });
        }

        await db.SaveChangesAsync();

        return Ok(new
        {
            announcementId = announcement.AnnouncementId,
            moduleId = request.ModuleId,
            moduleCode = module.ModuleCode,
            title,
            message,
            recipientCount = recipientIds.Count,
            createdAt = now
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetPublished()
    {
        var lecturer = await GetLecturerAsync();

        if (lecturer == null)
            return Unauthorized();

        var announcements = await (
            from announcement in db.Announcements.AsNoTracking()
            join module in db.Modules.AsNoTracking()
                on announcement.ModuleId equals (int?)module.ModuleId
                into moduleGroup
            from module in moduleGroup.DefaultIfEmpty()
            where announcement.CreatedByUserId == lecturer.UserId
            orderby announcement.CreatedAt descending
            select new
            {
                announcementId = announcement.AnnouncementId,
                moduleId = announcement.ModuleId,
                moduleCode = module != null ? module.ModuleCode : "",
                moduleName = module != null ? module.ModuleName : "",
                title = announcement.Title,
                message = announcement.Message,
                createdAt = announcement.CreatedAt
            }
        ).ToListAsync();

        return Ok(announcements);
    }

    private async Task<Lecturer?> GetLecturerAsync()
    {
        string? claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out int userId))
            return null;

        return await db.Lecturers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.UserId == userId);
    }
}