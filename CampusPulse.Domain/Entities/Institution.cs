namespace CampusPulse.Domain.Entities
{
    public class Institution
    {
        public int InstitutionId { get; set; }
        public string InstitutionName { get; set; } = "";
        public string StudentEmailDomain { get; set; } = "";
    }
}