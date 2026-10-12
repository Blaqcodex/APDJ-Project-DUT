
using System.ComponentModel.DataAnnotations;
using CampusPulse.API.Services;
using CampusPulse.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CampusPulse.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly PasswordResetService _passwordResetService;

        public AuthController(
            IRegistrationService registrationService,
            IAuthenticationService authenticationService,
            IJwtTokenService jwtTokenService,
            PasswordResetService passwordResetService)
        {
            _registrationService = registrationService;
            _authenticationService = authenticationService;
            _jwtTokenService = jwtTokenService;
            _passwordResetService = passwordResetService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            bool registered = _registrationService.RegisterStudent(
                request.FirstName,
                request.LastName,
                request.Email.Trim().ToLowerInvariant(),
                request.StudentNumber,
                request.Password,
                request.RecoveryEmail.Trim().ToLowerInvariant()
            );

            if (!registered)
            {
                return BadRequest(new
                {
                    message = "Registration failed. Check your DUT email, student number, recovery Gmail address, or existing account."
                });
            }

            return Ok(new
            {
                message = "Student registered successfully. Your recovery email still needs verification."
            });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var user = _authenticationService.Login(
                request.Email.Trim().ToLowerInvariant(),
                request.Password
            );

            if (user == null || user.Role != "Student")
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            string token = _jwtTokenService.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful.",
                token,
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                role = user.Role
            });
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordRequest request)
        {
            await _passwordResetService.SendResetEmailAsync(
                request.Email);

            return Ok(new
            {
                message = "If this email is registered and verified, password reset instructions will be sent."
            });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("forgot-password")]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordRequest request)
        {
            bool success = await _passwordResetService.ResetPasswordAsync(
                request.Email,
                request.Token,
                request.NewPassword);

            if (!success)
            {
                return BadRequest(new
                {
                    message = "This reset token is invalid or has expired."
                });
            }

            return Ok(new
            {
                message = "Password updated successfully. You can now log in."
            });
        }
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^\d{8}$",
            ErrorMessage = "Student number must contain exactly 8 digits.")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string RecoveryEmail { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Reset code must contain exactly six digits.")]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
