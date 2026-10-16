using System.Security.Claims;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

[ApiController]
[Route("api/student/notifications")]
[Authorize(Roles = "Student")]
public sealed class StudentNotificationsController : ControllerBase
{
    private readonly CampusPulseDbContext db;

    public StudentNotificationsController(CampusPulseDbContext db)
    {
        this.db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        int? userId = await GetStudentUserIdAsync();

        if (userId == null)
            return Unauthorized();

        var notifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.NotificationId)
            .Select(n => new
            {
                notificationId = n.NotificationId,
                title = n.Title,
                message = n.Message,
                createdAt = n.CreatedAt,
                isRead = n.IsRead
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        int? userId = await GetStudentUserIdAsync();

        if (userId == null)
            return Unauthorized();

        int count = await db.Notifications
            .CountAsync(n =>
                n.UserId == userId.Value &&
                !n.IsRead);

        return Ok(new { unreadCount = count });
    }

    [HttpPatch("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        int? userId = await GetStudentUserIdAsync();

        if (userId == null)
            return Unauthorized();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n =>
                n.NotificationId == notificationId &&
                n.UserId == userId.Value);

        if (notification == null)
            return NotFound(new
            {
                message = "Notification not found."
            });

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync();
        }

        return Ok(new
        {
            notificationId = notification.NotificationId,
            isRead = notification.IsRead
        });
    }

    private async Task<int?> GetStudentUserIdAsync()
    {
        string? claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out int userId))
            return null;

        bool studentExists = await db.Students
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId);

        return studentExists ? userId : null;
    }
}