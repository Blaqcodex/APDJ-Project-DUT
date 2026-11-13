namespace CampusPulse.Infrastructure.Configuration;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public string ProjectId { get; set; } = string.Empty;

    public string ServiceAccountPath { get; set; } = string.Empty;

    public string StorageBucket { get; set; } = string.Empty;

    public bool EnableLocalFallback { get; set; } = true;
}