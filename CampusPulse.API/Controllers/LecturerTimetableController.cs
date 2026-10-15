using System.Security.Claims;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers
{
    [ApiController]
    [Route("api/lecturer/timetable")]
    [Authorize(Roles = "Lecturer")]
    public sealed class LecturerTimetableController : ControllerBase
    {
        private readonly CampusPulseDbContext _db;

        public LecturerTimetableController(CampusPulseDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyClasses()
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int userId))
            {
                return Unauthorized();
            }

            var lecturer = await _db.Lecturers
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (lecturer == null)
            {
                return Unauthorized();
            }

            var classes = await (
                from timetable in _db.Timetables.AsNoTracking()
                join module in _db.Modules.AsNoTracking()
                    on timetable.ModuleId equals module.ModuleId
                join venue in _db.Venues.AsNoTracking()
                    on timetable.VenueId equals venue.VenueId
                where timetable.LecturerId == lecturer.LecturerId
                orderby timetable.DayOfWeek, timetable.StartTime
                select new
                {
                    timetableId = timetable.TimetableId,
                    moduleCode = module.ModuleCode,
                    moduleName = module.ModuleName,
                    dayOfWeek = timetable.DayOfWeek.ToString(),
                    startTime = timetable.StartTime.ToString(@"hh\:mm"),
                    endTime = timetable.EndTime.ToString(@"hh\:mm"),
                    venueName = venue.VenueName,
                    campus = venue.Campus
                }
            ).ToListAsync();

            return Ok(classes);
        }
    }
}