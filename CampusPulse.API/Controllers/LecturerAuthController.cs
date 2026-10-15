using System.ComponentModel.DataAnnotations;
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

[ApiController]
[Route("api/lecturer/auth")]
public class LecturerAuthController : ControllerBase
{
    private readonly IAuthenticationService authenticationService;
    private readonly IJwtTokenService jwtTokenService;
    private readonly CampusPulseDbContext db;

    public LecturerAuthController(
        IAuthenticationService authenticationService,
        IJwtTokenService jwtTokenService,
        CampusPulseDbContext db)
    {
        this.authenticationService = authenticationService;
        this.jwtTokenService = jwtTokenService;
        this.db = db;
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
            role = user.Role
        });
    }
}