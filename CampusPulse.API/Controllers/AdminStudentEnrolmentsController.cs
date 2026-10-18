using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

public sealed class AddStudentModuleRequest
{
    public int ModuleId { get; set; }
}
public sealed class AssignStudentProgrammeRequest
{
    public int ProgrammeId { get; set; }
}

[ApiController]
[Route("api/admin/student-enrolments")]
[Authorize(Roles = "Admin")]
public sealed class AdminStudentEnrolmentsController : ControllerBase
{
    private readonly CampusPulseDbContext db;

    public AdminStudentEnrolmentsController(CampusPulseDbContext db)
    {
        this.db = db;
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(
        CancellationToken cancellationToken)
    {
        var students = await (
            from student in db.Students.AsNoTracking()
            join user in db.Users.AsNoTracking()
                on student.UserId equals user.UserId
            join programme in db.Programmes.AsNoTracking()
                on student.ProgrammeId equals programme.ProgrammeId
                into programmeGroup
            from programme in programmeGroup.DefaultIfEmpty()
            orderby user.LastName, user.FirstName
            select new
            {
                studentId = student.StudentId,
                studentNumber = student.StudentNumber,
                firstName = user.FirstName,
                lastName = user.LastName,
                programmeId = student.ProgrammeId,
                programmeName = programme == null
                    ? "Not assigned"
                    : programme.ProgrammeName,
                yearLevel = student.YearLevel,
                isActive = user.IsActive
            }
        ).ToListAsync(cancellationToken);

        return Ok(students);
    }

    [HttpGet("students/{studentId:int}")]
    public async Task<IActionResult> GetStudentEnrolment(
        int studentId,
        CancellationToken cancellationToken)
    {
        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.StudentId == studentId,
                cancellationToken);

        if (student is null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var modules = await (
            from enrollment in db.StudentModules.AsNoTracking()
            join module in db.Modules.AsNoTracking()
                on enrollment.ModuleId equals module.ModuleId
            where enrollment.StudentId == studentId
            orderby module.ModuleCode
            select new
            {
                moduleId = module.ModuleId,
                moduleCode = module.ModuleCode,
                moduleName = module.ModuleName,
                programmeId = module.ProgrammeId
            }
        ).ToListAsync(cancellationToken);

        return Ok(new
        {
            studentId = student.StudentId,
            programmeId = student.ProgrammeId,
            yearLevel = student.YearLevel,
            modules
        });
    }

    [HttpPut("students/{studentId:int}/programme")]
    public async Task<IActionResult> AssignProgramme(
        int studentId,
        [FromBody] AssignStudentProgrammeRequest request,
        CancellationToken cancellationToken)
    {
        if (studentId <= 0 || request is null ||
            request.ProgrammeId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid student and programme are required."
            });
        }

        var student = await db.Students
            .FirstOrDefaultAsync(
                item => item.StudentId == studentId,
                cancellationToken);

        if (student is null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        bool programmeExists = await db.Programmes
            .AnyAsync(
                item => item.ProgrammeId == request.ProgrammeId,
                cancellationToken);

        if (!programmeExists)
        {
            return BadRequest(new
            {
                message = "The selected programme does not exist."
            });
        }

        // Reassigning the current programme must not undo deliberate
        // module removals or other manual enrolment adjustments.
        if (student.ProgrammeId == request.ProgrammeId)
        {
            return Ok(new
            {
                studentId,
                programmeId = request.ProgrammeId,
                addedModules = 0,
                alreadyEnrolledModules = await db.StudentModules
                    .AsNoTracking()
                    .CountAsync(
                        item => item.StudentId == studentId,
                        cancellationToken),
                message = "Programme already assigned; manual module selections preserved.",
                note = "No module enrolments or attendance records were changed."
            });
        }
        var programmeModuleIds = await db.Modules
            .AsNoTracking()
            .Where(item => item.ProgrammeId == request.ProgrammeId)
            .Select(item => item.ModuleId)
            .ToListAsync(cancellationToken);

        var existingModuleIds = await db.StudentModules
            .AsNoTracking()
            .Where(item => item.StudentId == studentId)
            .Select(item => item.ModuleId)
            .ToListAsync(cancellationToken);

        var existingSet = existingModuleIds.ToHashSet();

        var missingModuleIds = programmeModuleIds
            .Where(moduleId => !existingSet.Contains(moduleId))
            .ToList();

        student.ProgrammeId = request.ProgrammeId;

        foreach (int moduleId in missingModuleIds)
        {
            db.StudentModules.Add(new StudentModule
            {
                StudentId = studentId,
                ModuleId = moduleId
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            studentId,
            programmeId = request.ProgrammeId,
            addedModules = missingModuleIds.Count,
            alreadyEnrolledModules =
                programmeModuleIds.Count - missingModuleIds.Count,
            message = "Programme assigned and modules synchronised.",
            note = "Existing module enrolments and attendance history were preserved."
        });
    }

    [HttpPost("students/{studentId:int}/modules")]
    public async Task<IActionResult> AddStudentModule(
        int studentId,
        [FromBody] AddStudentModuleRequest request,
        CancellationToken cancellationToken)
    {
        if (studentId <= 0 || request is null || request.ModuleId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid student and module are required."
            });
        }

        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.StudentId == studentId,
                cancellationToken);

        if (student is null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var module = await db.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.ModuleId == request.ModuleId,
                cancellationToken);

        if (module is null)
        {
            return NotFound(new
            {
                message = "Module not found."
            });
        }

        if (student.ProgrammeId <= 0 ||
            module.ProgrammeId != student.ProgrammeId)
        {
            return BadRequest(new
            {
                message = "The module must belong to the student's assigned programme."
            });
        }

        bool alreadyEnrolled = await db.StudentModules
            .AnyAsync(
                item => item.StudentId == studentId &&
                        item.ModuleId == request.ModuleId,
                cancellationToken);

        if (alreadyEnrolled)
        {
            return Conflict(new
            {
                message = "Student is already enrolled in this module."
            });
        }

        db.StudentModules.Add(new StudentModule
        {
            StudentId = studentId,
            ModuleId = request.ModuleId
        });

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            studentId,
            moduleId = request.ModuleId,
            message = "Student enrolled in module."
        });
    }

    [HttpDelete("students/{studentId:int}/modules/{moduleId:int}")]
    public async Task<IActionResult> RemoveStudentModule(
        int studentId,
        int moduleId,
        CancellationToken cancellationToken)
    {
        if (studentId <= 0 || moduleId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid student and module are required."
            });
        }

        bool studentExists = await db.Students
            .AnyAsync(
                item => item.StudentId == studentId,
                cancellationToken);

        if (!studentExists)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var enrolment = await db.StudentModules
            .FirstOrDefaultAsync(
                item => item.StudentId == studentId &&
                        item.ModuleId == moduleId,
                cancellationToken);

        if (enrolment is null)
        {
            return NotFound(new
            {
                message = "Student is not enrolled in this module."
            });
        }

        db.StudentModules.Remove(enrolment);

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            studentId,
            moduleId,
            message = "Module enrolment removed.",
            note = "Historical attendance records were not deleted."
        });
    }
}