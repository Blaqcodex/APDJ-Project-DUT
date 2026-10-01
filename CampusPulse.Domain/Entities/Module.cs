namespace CampusPulse.Domain.Entities
{
    public class Module
    {
        public int ModuleId { get; set; }

        public string ModuleCode { get; set; } = "";

        public string ModuleName { get; set; } = "";

        public int ProgrammeId { get; set; }
    }
}