
using System.ComponentModel.DataAnnotations;
using CampusPulse.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CampusPulse.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(
            IRegistrationService registrationService,
            IAuthenticationService authenticationService,
            IJwtTokenService jwtTokenService)
        {
            _registrationService = registrationService;
            _authenticationService = authenticationService;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            bool registered = _registrationService.RegisterStudent(
                request.FirstName,
                request.LastName,
                request.Email,
                request.StudentNumber,
                request.Password
            );

            if (!registered)
            {
                return BadRequest(new
                {
                    message = "Registration failed. Check your details or whether the account already exists."
                });
            }

            return Ok(new
            {
                message = "Student registered successfully."
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
                token = token,
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                role = user.Role
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
        [RegularExpression(@"^\d{8}$",
            ErrorMessage = "Student number must contain exactly 8 digits.")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
