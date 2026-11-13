using CampusPulse.Application.DTOs.Learning;
using CampusPulse.Application.Interfaces;
using CampusPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.Infrastructure.Services;

public sealed class ModuleAccessService : IModuleAccessService
{
    private readonly CampusPulseDbContext _db;

    public ModuleAccessService(CampusPulseDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AccessibleModuleDto>> GetAccessibleModulesAsync(
        int userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(role))
        {
            return Array.Empty<AccessibleModuleDto>();
        }

        bool isActiveUser = await _db.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.UserId == userId && u.IsActive && u.Role == role,
                cancellationToken);

        if (!isActiveUser)
        {
            return Array.Empty<AccessibleModuleDto>();
        }

        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            int? studentId = await _db.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => (int?)s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!studentId.HasValue)
            {
                return Array.Empty<AccessibleModuleDto>();
            }

            return await (
                from enrollment in _db.StudentModules.AsNoTracking()
                join module in _db.Modules.AsNoTracking()
                    on enrollment.ModuleId equals module.ModuleId
                where enrollment.StudentId == studentId.Value
                select new AccessibleModuleDto
                {
                    ModuleId = module.ModuleId,
                    ModuleCode = module.ModuleCode,
                    ModuleName = module.ModuleName,
                    ProgrammeId = module.ProgrammeId
                })
                .Distinct()
                .OrderBy(m => m.ModuleCode)
                .ToListAsync(cancellationToken);
        }

        if (string.Equals(role, "Lecturer", StringComparison.OrdinalIgnoreCase))
        {
            int? lecturerId = await _db.Lecturers
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .Select(l => (int?)l.LecturerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!lecturerId.HasValue)
            {
                return Array.Empty<AccessibleModuleDto>();
            }

            return await (
                from assignment in _db.LecturerModules.AsNoTracking()
                join module in _db.Modules.AsNoTracking()
                    on assignment.ModuleId equals module.ModuleId
                where assignment.LecturerId == lecturerId.Value
                select new AccessibleModuleDto
                {
                    ModuleId = module.ModuleId,
                    ModuleCode = module.ModuleCode,
                    ModuleName = module.ModuleName,
                    ProgrammeId = module.ProgrammeId
                })
                .Distinct()
                .OrderBy(m => m.ModuleCode)
                .ToListAsync(cancellationToken);
        }

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return await _db.Modules
                .AsNoTracking()
                .OrderBy(m => m.ModuleCode)
                .Select(m => new AccessibleModuleDto
                {
                    ModuleId = m.ModuleId,
                    ModuleCode = m.ModuleCode,
                    ModuleName = m.ModuleName,
                    ProgrammeId = m.ProgrammeId
                })
                .ToListAsync(cancellationToken);
        }

        return Array.Empty<AccessibleModuleDto>();
    }

    public async Task<bool> CanAccessModuleAsync(
        int userId,
        string role,
        int moduleId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || moduleId <= 0 || string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        bool isActiveUser = await _db.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.UserId == userId && u.IsActive && u.Role == role,
                cancellationToken);

        if (!isActiveUser)
        {
            return false;
        }

        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return await (
                from student in _db.Students.AsNoTracking()
                join enrollment in _db.StudentModules.AsNoTracking()
                    on student.StudentId equals enrollment.StudentId
                where student.UserId == userId && enrollment.ModuleId == moduleId
                select enrollment.StudentModuleId)
                .AnyAsync(cancellationToken);
        }

        if (string.Equals(role, "Lecturer", StringComparison.OrdinalIgnoreCase))
        {
            return await (
                from lecturer in _db.Lecturers.AsNoTracking()
                join assignment in _db.LecturerModules.AsNoTracking()
                    on lecturer.LecturerId equals assignment.LecturerId
                where lecturer.UserId == userId && assignment.ModuleId == moduleId
                select assignment.LecturerModuleId)
                .AnyAsync(cancellationToken);
        }

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return await _db.Modules
                .AsNoTracking()
                .AnyAsync(m => m.ModuleId == moduleId, cancellationToken);
        }

        return false;
    }

    public async Task<bool> CanManageModuleContentAsync(
        int userId,
        string role,
        int moduleId,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(role, "Lecturer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return await CanAccessModuleAsync(userId, role, moduleId, cancellationToken);
        }

        return false;
    }
}