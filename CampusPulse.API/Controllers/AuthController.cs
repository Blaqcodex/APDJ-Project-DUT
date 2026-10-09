
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
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}