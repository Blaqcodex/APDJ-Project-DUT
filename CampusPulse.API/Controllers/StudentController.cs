
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusPulse.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            string? userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            string? email = User.FindFirstValue(
                ClaimTypes.Email);

            return Ok(new
            {
                message = "Student profile accessed successfully.",
                userId = userId,
                email = email
            });
        }
    }
}
