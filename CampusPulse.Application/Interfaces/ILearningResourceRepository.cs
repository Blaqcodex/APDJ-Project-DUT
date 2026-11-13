using CampusPulse.Application.DTOs.Learning;

namespace CampusPulse.Application.Interfaces;

public interface ILearningResourceRepository
{
    Task<IReadOnlyList<LearningResourceDto>> GetByModuleAsync(
        int moduleId,
        bool includeUnpublished,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LearningResourceDto>> GetByModulesAsync(
        IEnumerable<int> moduleIds,
        bool includeUnpublished,
        CancellationToken cancellationToken = default);

    Task<LearningResourceDto?> GetByIdAsync(
        string resourceId,
        CancellationToken cancellationToken = default);

    Task<LearningResourceDto> CreateAsync(
        LearningResourceDto resource,
        CancellationToken cancellationToken = default);

    Task<LearningResourceDto?> UpdateAsync(
        LearningResourceDto resource,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string resourceId,
        CancellationToken cancellationToken = default);
}