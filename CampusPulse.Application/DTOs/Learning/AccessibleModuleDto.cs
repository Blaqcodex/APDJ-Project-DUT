namespace CampusPulse.Application.DTOs.Learning;

public sealed class AccessibleModuleDto
{
    public int ModuleId { get; set; }

    public string ModuleCode { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public int ProgrammeId { get; set; }
}