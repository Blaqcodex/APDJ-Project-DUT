using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers
{
    public sealed class CreateAdminProgrammeRequest
    {
        public string ProgrammeName { get; set; } = "";
        public string Faculty { get; set; } = "";
    }

    [ApiController]
    [Route("api/admin/programmes")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminProgrammesController : ControllerBase
    {
        private readonly CampusPulseDbContext _db;

        public AdminProgrammesController(CampusPulseDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetProgrammes(
            CancellationToken cancellationToken)
        {
            var programmes = await _db.Programmes
                .AsNoTracking()
                .OrderBy(p => p.Faculty)
                .ThenBy(p => p.ProgrammeName)
                .Select(p => new
                {
                    programmeId = p.ProgrammeId,
                    programmeName = p.ProgrammeName,
                    faculty = p.Faculty
                })
                .ToListAsync(cancellationToken);

            return Ok(programmes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProgramme(
            [FromBody] CreateAdminProgrammeRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "A programme request is required."
                });
            }

            string name = (request.ProgrammeName ?? "").Trim();
            string faculty = (request.Faculty ?? "").Trim();

            if (name.Length == 0 || name.Length > 150)
            {
                return BadRequest(new
                {
                    message = "Programme name must contain 1 to 150 characters."
                });
            }

            if (faculty.Length == 0 || faculty.Length > 150)
            {
                return BadRequest(new
                {
                    message = "Faculty must contain 1 to 150 characters."
                });
            }

            bool duplicateExists = await _db.Programmes
                .AnyAsync(
                    p => p.ProgrammeName.ToUpper() == name.ToUpper() &&
                         p.Faculty.ToUpper() == faculty.ToUpper(),
                    cancellationToken);

            if (duplicateExists)
            {
                return Conflict(new
                {
                    message = "This programme already exists in the selected faculty."
                });
            }

            var programme = new Programme
            {
                ProgrammeName = name,
                Faculty = faculty
            };

            _db.Programmes.Add(programme);

            await _db.SaveChangesAsync(cancellationToken);

            return StatusCode(201, new
            {
                programmeId = programme.ProgrammeId,
                programmeName = programme.ProgrammeName,
                faculty = programme.Faculty
            });
        }
    }
}