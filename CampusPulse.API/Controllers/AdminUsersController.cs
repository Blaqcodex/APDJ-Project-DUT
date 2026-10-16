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
                createdAt = user.CreatedAt
            })
            .ToListAsync();

        return Ok(results);
    }
}