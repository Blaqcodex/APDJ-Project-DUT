namespace CampusPulse.Application.DTOs.Learning;

public static class LearningResourceTypes
{
    public const string Notes = "Notes";
    public const string Slides = "Slides";
    public const string PastPaper = "PastPaper";
    public const string StudyGuide = "StudyGuide";
    public const string ExternalLink = "ExternalLink";

    public static readonly IReadOnlyList<string> All =
    [
        Notes,
        Slides,
        PastPaper,
        StudyGuide,
        ExternalLink
    ];

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        foreach (string candidate in All)
        {
            if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                normalized = candidate;
                return true;
            }
        }

        return false;
    }
}

public sealed class LearningResourceDto
{
    public string ResourceId { get; set; } = string.Empty;

    public int ModuleId { get; set; }

    public string ModuleCode { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ResourceType { get; set; } = LearningResourceTypes.Notes;

    public string ContentText { get; set; } = string.Empty;

    public string ExternalUrl { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public int CreatedByUserId { get; set; }

    public string CreatedByName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsPublished { get; set; } = true;
}

public sealed class CreateLearningResourceRequest
{
    public int ModuleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ResourceType { get; set; } = LearningResourceTypes.Notes;

    public string ContentText { get; set; } = string.Empty;

    public string ExternalUrl { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
}

public sealed class UpdateLearningResourceRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ResourceType { get; set; } = LearningResourceTypes.Notes;

    public string ContentText { get; set; } = string.Empty;

    public string ExternalUrl { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
}