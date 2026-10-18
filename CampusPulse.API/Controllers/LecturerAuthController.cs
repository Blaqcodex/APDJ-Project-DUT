using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CampusPulse.Application.Interfaces;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

public sealed class LecturerLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public sealed class LecturerChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = "";

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = "";
}

[ApiController]
[Route("api/lecturer/auth")]
public class LecturerAuthController : ControllerBase
{
    private readonly IAuthenticationService authenticationService;
    private readonly IJwtTokenService jwtTokenService;
    private readonly CampusPulseDbContext db;
    private readonly IPasswordService passwordService;

    public LecturerAuthController(
        IAuthenticationService authenticationService,
        IJwtTokenService jwtTokenService,
        CampusPulseDbContext db,
        IPasswordService passwordService)
    {
        this.authenticationService = authenticationService;
        this.jwtTokenService = jwtTokenService;
        this.db = db;
        this.passwordService = passwordService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LecturerLoginRequest request)
    {
        var user = authenticationService.Login(
            request.Email.Trim().ToLowerInvariant(),
            request.Password);

        if (user == null || user.Role != "Lecturer")
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        bool lecturerExists = await db.Lecturers
            .AnyAsync(l => l.UserId == user.UserId);

        if (!lecturerExists)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        string token = jwtTokenService.GenerateToken(user);

        return Ok(new
        {
            message = "Lecturer login successful.",
            token,
            userId = user.UserId,
            firstName = user.FirstName,
            lastName = user.LastName,
            role = user.Role,
            mustChangePassword = user.MustChangePassword
        });
    }

    [Authorize(Roles = "Lecturer")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] LecturerChangePasswordRequest request)
    {
        string? userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out int userId))
        {
            return Unauthorized();
        }

        var user = await db.Users
            .SingleOrDefaultAsync(u =>
                u.UserId == userId &&
                u.Role == "Lecturer" &&
                u.IsActive);

        if (user == null)
        {
            return Unauthorized();
        }

        bool currentPasswordValid = passwordService.VerifyPassword(
            request.CurrentPassword,
            user.PasswordHash);

        if (!currentPasswordValid)
        {
            return BadRequest(new
            {
                message = "Current password is incorrect."
            });
        }

        if (passwordService.VerifyPassword(
            request.NewPassword,
            user.PasswordHash))
        {
            return BadRequest(new
            {
                message = "New password must differ from the current password."
            });
        }

        user.PasswordHash = passwordService.HashPassword(
            request.NewPassword);

        user.MustChangePassword = false;

        await db.SaveChangesAsync();

        return Ok(new
        {
            message = "Password changed successfully.",
            mustChangePassword = false
        });
    }
}