using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly CampusPulseDbContext db;

    public AdminUsersController(CampusPulseDbContext db)
    {
        this.db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? role = null)
    {
        if (!string.IsNullOrWhiteSpace(role) &&
            role != "Student" &&
            role != "Lecturer" &&
            role != "Admin")
        {
            return BadRequest(new
            {
                message = "Invalid role filter."
            });
        }

        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(role))
        {
            users = users.Where(user => user.Role == role);
        }

        var results = await users
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .Select(user => new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role,
                createdAt = user.CreatedAt,
                isActive = user.IsActive
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpPatch("{userId:int}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        int userId,
        [FromBody] UpdateUserStatusRequest request)
    {
        if (request?.IsActive is null)
        {
            return BadRequest(new
            {
                message = "isActive must be true or false."
            });
        }

        var user = await db.Users
            .FirstOrDefaultAsync(item => item.UserId == userId);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        bool newStatus = request.IsActive.Value;

        if (!newStatus && user.Role == "Admin")
        {
            var currentUserId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value;

            if (int.TryParse(currentUserId, out int adminId) &&
                adminId == userId)
            {
                return Conflict(new
                {
                    message = "You cannot deactivate your own administrator account."
                });
            }

            int activeAdminCount = await db.Users
                .CountAsync(item =>
                    item.Role == "Admin" &&
                    item.IsActive);

            if (activeAdminCount <= 1)
            {
                return Conflict(new
                {
                    message = "The last active administrator cannot be deactivated."
                });
            }
        }

        if (user.IsActive == newStatus)
        {
            return Ok(new
            {
                userId = user.UserId,
                isActive = user.IsActive,
                message = "Account status is already up to date."
            });
        }

        user.IsActive = newStatus;

        await db.SaveChangesAsync();

        return Ok(new
        {
            userId = user.UserId,
            isActive = user.IsActive,
            message = newStatus
                ? "User account reactivated."
                : "User account deactivated."
        });
    }
}

public sealed class UpdateUserStatusRequest
{
    public bool? IsActive { get; set; }
}