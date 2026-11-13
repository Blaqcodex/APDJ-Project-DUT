using CampusPulse.Application.DTOs.Learning;

namespace CampusPulse.Application.Interfaces;

public interface IModuleAccessService
{
    Task<IReadOnlyList<AccessibleModuleDto>> GetAccessibleModulesAsync(
        int userId,
        string role,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessModuleAsync(
        int userId,
        string role,
        int moduleId,
        CancellationToken cancellationToken = default);

    Task<bool> CanManageModuleContentAsync(
        int userId,
        string role,
        int moduleId,
        CancellationToken cancellationToken = default);
}