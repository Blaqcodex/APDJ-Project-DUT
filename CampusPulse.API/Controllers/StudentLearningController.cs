using System.Security.Claims;
using CampusPulse.Application.DTOs.Learning;
using CampusPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusPulse.API.Controllers;

[ApiController]
[Route("api/student/learning")]
[Authorize(Roles = "Student")]
public sealed class StudentLearningController : ControllerBase
{
    private readonly IModuleAccessService _moduleAccess;
    private readonly ILearningResourceRepository _resources;

    public StudentLearningController(
        IModuleAccessService moduleAccess,
        ILearningResourceRepository resources)
    {
        _moduleAccess = moduleAccess;
        _resources = resources;
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetEnrolledModules(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        IReadOnlyList<AccessibleModuleDto> modules =
            await _moduleAccess.GetAccessibleModulesAsync(userId, "Student", cancellationToken);

        if (modules.Count == 0)
        {
            return Ok(Array.Empty<object>());
        }

        IReadOnlyList<LearningResourceDto> publishedResources =
            await _resources.GetByModulesAsync(
                modules.Select(m => m.ModuleId),
                includeUnpublished: false,
                cancellationToken);

        var countsByModule = publishedResources
            .GroupBy(r => r.ModuleId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = modules.Select(m => new
        {
            m.ModuleId,
            m.ModuleCode,
            m.ModuleName,
            m.ProgrammeId,
            ResourceCount = countsByModule.GetValueOrDefault(m.ModuleId, 0)
        });

        return Ok(result);
    }

    [HttpGet("modules/{moduleId:int}/resources")]
    public async Task<IActionResult> GetModuleResources(
        int moduleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        if (moduleId <= 0)
        {
            return BadRequest(new { message = "ModuleId must be a positive integer." });
        }

        bool canAccess = await _moduleAccess.CanAccessModuleAsync(
            userId,
            "Student",
            moduleId,
            cancellationToken);

        if (!canAccess)
        {
            return Forbid();
        }

        IReadOnlyList<LearningResourceDto> items =
            await _resources.GetByModuleAsync(
                moduleId,
                includeUnpublished: false,
                cancellationToken);

        return Ok(items);
    }

    [HttpGet("resources/recent")]
    public async Task<IActionResult> GetRecentResources(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        int boundedTake = Math.Clamp(take, 1, 50);

        IReadOnlyList<AccessibleModuleDto> modules =
            await _moduleAccess.GetAccessibleModulesAsync(userId, "Student", cancellationToken);

        if (modules.Count == 0)
        {
            return Ok(Array.Empty<LearningResourceDto>());
        }

        IReadOnlyList<LearningResourceDto> items =
            await _resources.GetByModulesAsync(
                modules.Select(m => m.ModuleId),
                includeUnpublished: false,
                cancellationToken);

        return Ok(items.Take(boundedTake).ToList());
    }

    [HttpGet("resources/{resourceId}")]
    public async Task<IActionResult> GetResourceById(
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

        LearningResourceDto? item = await _resources.GetByIdAsync(resourceId, cancellationToken);
        if (item is null || !item.IsPublished)
        {
            return NotFound(new { message = "Learning resource was not found." });
        }

        bool canAccess = await _moduleAccess.CanAccessModuleAsync(
            userId,
            "Student",
            item.ModuleId,
            cancellationToken);

        if (!canAccess)
        {
            return Forbid();
        }

        return Ok(item);
    }

    private bool TryGetUserId(out int userId)
    {
        string? claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}