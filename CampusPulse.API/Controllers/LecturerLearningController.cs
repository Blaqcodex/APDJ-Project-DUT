using System.Security.Claims;
using CampusPulse.Application.DTOs.Learning;
using CampusPulse.Application.Interfaces;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

[ApiController]
[Route("api/lecturer/learning")]
[Authorize(Roles = "Lecturer")]
public sealed class LecturerLearningController : ControllerBase
{
    private readonly CampusPulseDbContext _db;
    private readonly IModuleAccessService _moduleAccess;
    private readonly ILearningResourceRepository _resources;

    public LecturerLearningController(
        CampusPulseDbContext db,
        IModuleAccessService moduleAccess,
        ILearningResourceRepository resources)
    {
        _db = db;
        _moduleAccess = moduleAccess;
        _resources = resources;
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetAssignedModules(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        IReadOnlyList<AccessibleModuleDto> modules =
            await _moduleAccess.GetAccessibleModulesAsync(userId, "Lecturer", cancellationToken);

        return Ok(modules);
    }

    [HttpGet("resources")]
    public async Task<IActionResult> GetResources(
        [FromQuery] int? moduleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        if (moduleId.HasValue)
        {
            if (moduleId.Value <= 0)
            {
                return BadRequest(new { message = "ModuleId must be a positive integer." });
            }

            bool canManage = await _moduleAccess.CanManageModuleContentAsync(
                userId,
                "Lecturer",
                moduleId.Value,
                cancellationToken);

            if (!canManage)
            {
                return Forbid();
            }

            IReadOnlyList<LearningResourceDto> moduleResources =
                await _resources.GetByModuleAsync(
                    moduleId.Value,
                    includeUnpublished: true,
                    cancellationToken);

            return Ok(moduleResources);
        }

        IReadOnlyList<AccessibleModuleDto> modules =
            await _moduleAccess.GetAccessibleModulesAsync(userId, "Lecturer", cancellationToken);

        if (modules.Count == 0)
        {
            return Ok(Array.Empty<LearningResourceDto>());
        }

        IReadOnlyList<LearningResourceDto> allResources =
            await _resources.GetByModulesAsync(
                modules.Select(m => m.ModuleId),
                includeUnpublished: true,
                cancellationToken);

        return Ok(allResources);
    }

    [HttpPost("resources")]
    public async Task<IActionResult> CreateResource(
        [FromBody] CreateLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        if (request.ModuleId <= 0 || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "ModuleId and Title are required." });
        }

        if (!LearningResourceTypes.TryNormalize(request.ResourceType, out string normalizedType))
        {
            return BadRequest(new
            {
                message = $"ResourceType must be one of: {string.Join(", ", LearningResourceTypes.All)}."
            });
        }

        string title = request.Title.Trim();
        string description = (request.Description ?? string.Empty).Trim();
        string contentText = (request.ContentText ?? string.Empty).Trim();
        string externalUrl = (request.ExternalUrl ?? string.Empty).Trim();

        if (title.Length > 160 || description.Length > 2000 || contentText.Length > 20000)
        {
            return BadRequest(new
            {
                message = "Title (max 160), description (max 2000), or content (max 20000) exceeds length limits."
            });
        }

        if (string.IsNullOrWhiteSpace(contentText) && string.IsNullOrWhiteSpace(externalUrl))
        {
            return BadRequest(new
            {
                message = "Provide either study content text or an external resource URL."
            });
        }

        if (!string.IsNullOrWhiteSpace(externalUrl) && !IsValidHttpUrl(externalUrl))
        {
            return BadRequest(new
            {
                message = "ExternalUrl must be a valid HTTP or HTTPS URL."
            });
        }

        bool canManage = await _moduleAccess.CanManageModuleContentAsync(
            userId,
            "Lecturer",
            request.ModuleId,
            cancellationToken);

        if (!canManage)
        {
            return Forbid();
        }

        var module = await _db.Modules
            .AsNoTracking()
            .Where(m => m.ModuleId == request.ModuleId)
            .Select(m => new { m.ModuleId, m.ModuleCode, m.ModuleName })
            .FirstOrDefaultAsync(cancellationToken);

        if (module is null)
        {
            return NotFound(new { message = "Module was not found." });
        }

        var lecturerUser = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        string createdByName = lecturerUser is null
            ? "Lecturer"
            : $"{lecturerUser.FirstName} {lecturerUser.LastName}".Trim();

        var resource = new LearningResourceDto
        {
            ModuleId = module.ModuleId,
            ModuleCode = module.ModuleCode,
            ModuleName = module.ModuleName,
            Title = title,
            Description = description,
            ResourceType = normalizedType,
            ContentText = contentText,
            ExternalUrl = externalUrl,
            CreatedByUserId = userId,
            CreatedByName = createdByName,
            IsPublished = request.IsPublished
        };

        LearningResourceDto created = await _resources.CreateAsync(resource, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("resources/{resourceId}")]
    public async Task<IActionResult> UpdateResource(
        string resourceId,
        [FromBody] UpdateLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return BadRequest(new { message = "ResourceId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Title is required." });
        }

        if (!LearningResourceTypes.TryNormalize(request.ResourceType, out string normalizedType))
        {
            return BadRequest(new
            {
                message = $"ResourceType must be one of: {string.Join(", ", LearningResourceTypes.All)}."
            });
        }

        string title = request.Title.Trim();
        string description = (request.Description ?? string.Empty).Trim();
        string contentText = (request.ContentText ?? string.Empty).Trim();
        string externalUrl = (request.ExternalUrl ?? string.Empty).Trim();

        if (title.Length > 160 || description.Length > 2000 || contentText.Length > 20000)
        {
            return BadRequest(new
            {
                message = "Title (max 160), description (max 2000), or content (max 20000) exceeds length limits."
            });
        }

        if (string.IsNullOrWhiteSpace(contentText) && string.IsNullOrWhiteSpace(externalUrl))
        {
            return BadRequest(new
            {
                message = "Provide either study content text or an external resource URL."
            });
        }

        if (!string.IsNullOrWhiteSpace(externalUrl) && !IsValidHttpUrl(externalUrl))
        {
            return BadRequest(new
            {
                message = "ExternalUrl must be a valid HTTP or HTTPS URL."
            });
        }

        LearningResourceDto? existing = await _resources.GetByIdAsync(resourceId, cancellationToken);
        if (existing is null)
        {
            return NotFound(new { message = "Learning resource was not found." });
        }

        bool canManage = await _moduleAccess.CanManageModuleContentAsync(
            userId,
            "Lecturer",
            existing.ModuleId,
            cancellationToken);

        if (!canManage)
        {
            return Forbid();
        }

        existing.Title = title;
        existing.Description = description;
        existing.ResourceType = normalizedType;
        existing.ContentText = contentText;
        existing.ExternalUrl = externalUrl;
        existing.IsPublished = request.IsPublished;

        LearningResourceDto? updated = await _resources.UpdateAsync(existing, cancellationToken);
        if (updated is null)
        {
            return NotFound(new { message = "Learning resource was not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("resources/{resourceId}")]
    public async Task<IActionResult> DeleteResource(
        string resourceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return BadRequest(new { message = "ResourceId is required." });
        }

        LearningResourceDto? existing = await _resources.GetByIdAsync(resourceId, cancellationToken);
        if (existing is null)
        {
            return NotFound(new { message = "Learning resource was not found." });
        }

        bool canManage = await _moduleAccess.CanManageModuleContentAsync(
            userId,
            "Lecturer",
            existing.ModuleId,
            cancellationToken);

        if (!canManage)
        {
            return Forbid();
        }

        bool deleted = await _resources.DeleteAsync(resourceId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Learning resource was not found." });
        }

        return Ok(new { message = "Learning resource deleted." });
    }

    private bool TryGetUserId(out int userId)
    {
        string? claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }

    private static bool IsValidHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}