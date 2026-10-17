using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers
{
    public sealed class CreateAdminModuleRequest
    {
        public string ModuleCode { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public int ProgrammeId { get; set; }
    }

    [ApiController]
    [Route("api/admin/modules")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminModulesController : ControllerBase
    {
        private readonly CampusPulseDbContext _db;

        public AdminModulesController(CampusPulseDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetModules(
            CancellationToken cancellationToken)
        {
            var modules = await (
                from module in _db.Modules.AsNoTracking()
                join programme in _db.Programmes.AsNoTracking()
                    on module.ProgrammeId equals programme.ProgrammeId
                orderby module.ModuleCode, module.ModuleName
                select new
                {
                    moduleId = module.ModuleId,
                    moduleCode = module.ModuleCode,
                    moduleName = module.ModuleName,
                    programmeId = programme.ProgrammeId,
                    programmeName = programme.ProgrammeName,
                    faculty = programme.Faculty
                }
            ).ToListAsync(cancellationToken);

            return Ok(modules);
        }

        [HttpPost]
        public async Task<IActionResult> CreateModule(
            [FromBody] CreateAdminModuleRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "A module request is required."
                });
            }

            var code = (request.ModuleCode ?? "").Trim().ToUpperInvariant();
            var name = (request.ModuleName ?? "").Trim();

            if (code.Length == 0 || code.Length > 30)
            {
                return BadRequest(new
                {
                    message = "Module code must contain 1 to 30 characters."
                });
            }

            if (name.Length == 0 || name.Length > 150)
            {
                return BadRequest(new
                {
                    message = "Module name must contain 1 to 150 characters."
                });
            }

            if (request.ProgrammeId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid programme is required."
                });
            }

            bool programmeExists = await _db.Programmes
                .AnyAsync(
                    p => p.ProgrammeId == request.ProgrammeId,
                    cancellationToken);

            if (!programmeExists)
            {
                return BadRequest(new
                {
                    message = "The selected programme does not exist."
                });
            }

            bool duplicateExists = await _db.Modules
                .AnyAsync(
                    m => m.ProgrammeId == request.ProgrammeId &&
                         m.ModuleCode.ToUpper() == code,
                    cancellationToken);

            if (duplicateExists)
            {
                return Conflict(new
                {
                    message = "This module code already exists in the selected programme."
                });
            }

            var module = new Module
            {
                ModuleCode = code,
                ModuleName = name,
                ProgrammeId = request.ProgrammeId
            };

            _db.Modules.Add(module);

            await _db.SaveChangesAsync(cancellationToken);

            return StatusCode(201, new
            {
                moduleId = module.ModuleId,
                moduleCode = module.ModuleCode,
                moduleName = module.ModuleName,
                programmeId = module.ProgrammeId
            });
        }
    }
}