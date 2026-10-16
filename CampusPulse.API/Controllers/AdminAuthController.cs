using System.ComponentModel.DataAnnotations;
using CampusPulse.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CampusPulse.API.Controllers;

public sealed class AdminLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly IAuthenticationService authenticationService;
    private readonly IJwtTokenService jwtTokenService;

    public AdminAuthController(
        IAuthenticationService authenticationService,
        IJwtTokenService jwtTokenService)
    {
        this.authenticationService = authenticationService;
        this.jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("forgot-password")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        var user = authenticationService.Login(
            request.Email.Trim().ToLowerInvariant(),
            request.Password);

        if (user == null ||
            !string.Equals(
                user.Role,
                "Admin",
                StringComparison.Ordinal))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var token = jwtTokenService.GenerateToken(user);

        return Ok(new
        {
            message = "Administrator login successful.",
            token,
            userId = user.UserId,
            firstName = user.FirstName,
            lastName = user.LastName,
            role = user.Role
        });
    }
}