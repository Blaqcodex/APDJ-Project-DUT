using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers
{
    [ApiController]
    [Route("api/admin/lecturer-assignments")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminLecturerAssignmentsController : ControllerBase
    {
        private readonly CampusPulseDbContext _db;

        public AdminLecturerAssignmentsController(CampusPulseDbContext db)
        {
            _db = db;
        }

        [HttpGet("lecturers")]
        public async Task<IActionResult> GetLecturers()
        {
            var lecturers = await (
                from lecturer in _db.Lecturers.AsNoTracking()
                join user in _db.Users.AsNoTracking()
                    on lecturer.UserId equals user.UserId
                where user.Role == "Lecturer" && user.IsActive
                orderby user.LastName, user.FirstName
                select new
                {
                    lecturer.LecturerId,
                    lecturer.StaffNumber,
                    lecturer.Department,
                    user.FirstName,
                    user.LastName,
                    user.Email
                }
            ).ToListAsync();

            return Ok(lecturers);
        }

        [HttpGet]
        public async Task<IActionResult> GetAssignments()
        {
            var assignments = await (
                from assignment in _db.LecturerModules.AsNoTracking()
                join lecturer in _db.Lecturers.AsNoTracking()
                    on assignment.LecturerId equals lecturer.LecturerId
                join user in _db.Users.AsNoTracking()
                    on lecturer.UserId equals user.UserId
                join module in _db.Modules.AsNoTracking()
                    on assignment.ModuleId equals module.ModuleId
                orderby module.ModuleCode, user.LastName, user.FirstName
                select new
                {
                    assignment.LecturerModuleId,
                    assignment.LecturerId,
                    assignment.ModuleId,
                    module.ModuleCode,
                    module.ModuleName,
                    lecturer.StaffNumber,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.IsActive
                }
            ).ToListAsync();

            return Ok(assignments);
        }

        [HttpPost]
        public async Task<IActionResult> AssignLecturer(
            [FromBody] AssignLecturerRequest request)
        {
            if (request == null ||
                request.LecturerId <= 0 ||
                request.ModuleId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid lecturer and module are required."
                });
            }

            var lecturerExists = await (
                from lecturer in _db.Lecturers
                join user in _db.Users
                    on lecturer.UserId equals user.UserId
                where lecturer.LecturerId == request.LecturerId
                    && user.Role == "Lecturer"
                    && user.IsActive
                select lecturer.LecturerId
            ).AnyAsync();

            if (!lecturerExists)
            {
                return BadRequest(new
                {
                    message = "Lecturer not found or account is inactive."
                });
            }

            if (!await _db.Modules.AnyAsync(
                m => m.ModuleId == request.ModuleId))
            {
                return BadRequest(new
                {
                    message = "Selected module does not exist."
                });
            }

            var duplicate = await _db.LecturerModules.AnyAsync(
                lm => lm.LecturerId == request.LecturerId
                   && lm.ModuleId == request.ModuleId);

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "This lecturer is already assigned to the module."
                });
            }

            var assignment = new LecturerModule
            {
                LecturerId = request.LecturerId,
                ModuleId = request.ModuleId
            };

            _db.LecturerModules.Add(assignment);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique database index also prevents duplicates
                // when concurrent requests race to create an assignment.
                var nowExists = await _db.LecturerModules
                    .AsNoTracking()
                    .AnyAsync(lm =>
                        lm.LecturerId == request.LecturerId &&
                        lm.ModuleId == request.ModuleId);

                if (nowExists)
                {
                    return Conflict(new
                    {
                        message = "This lecturer is already assigned to the module."
                    });
                }

                throw;
            }

            return Created(
                $"api/admin/lecturer-assignments/{assignment.LecturerModuleId}",
                new
                {
                    assignment.LecturerModuleId,
                    assignment.LecturerId,
                    assignment.ModuleId
                });
        }

        [HttpDelete("{assignmentId:int}")]
        public async Task<IActionResult> RemoveAssignment(int assignmentId)
        {
            var assignment = await _db.LecturerModules
                .FirstOrDefaultAsync(
                    lm => lm.LecturerModuleId == assignmentId);

            if (assignment == null)
            {
                return NotFound(new
                {
                    message = "Assignment not found."
                });
            }

            var hasScheduledClasses = await _db.Timetables.AnyAsync(
                t => t.LecturerId == assignment.LecturerId
                  && t.ModuleId == assignment.ModuleId);

            if (hasScheduledClasses)
            {
                return Conflict(new
                {
                    message = "This lecturer has scheduled classes for the module. Update the timetable before removing the assignment."
                });
            }

            _db.LecturerModules.Remove(assignment);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        public sealed class AssignLecturerRequest
        {
            public int LecturerId { get; set; }

            public int ModuleId { get; set; }
        }
    }
}
