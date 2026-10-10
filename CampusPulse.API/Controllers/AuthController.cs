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

        public AuthController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
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
}