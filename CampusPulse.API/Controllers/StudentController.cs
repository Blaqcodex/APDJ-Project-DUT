using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusPulse.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly CampusPulseDbContext dbContext;

        public StudentController(CampusPulseDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            string? userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var profile = await (
                from student in dbContext.Students
                join user in dbContext.Users
                    on student.UserId equals user.UserId
                join programmeRecord in dbContext.Programmes
                    on student.ProgrammeId equals programmeRecord.ProgrammeId
                    into programmeGroup
                from programme in programmeGroup.DefaultIfEmpty()
                join institution in dbContext.Institutions
                    on student.InstitutionId equals institution.InstitutionId
                where student.UserId == userId
                select new
                {
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    student.StudentNumber,
                    ProgrammeName = programme == null ? null : programme.ProgrammeName,
                    Faculty = programme == null ? null : programme.Faculty,
                    YearLevel = student.YearLevel == 0 ? (int?)null : student.YearLevel,
                    institution.InstitutionName
                }
            ).AsNoTracking().FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound(new
                {
                    message = "Student profile was not found."
                });
            }

            return Ok(profile);
        }
    }
}
