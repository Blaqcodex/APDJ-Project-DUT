namespace CampusPulse.Domain.Entities
{
    public class Programme
    {
        public int ProgrammeId { get; set; }

        public string ProgrammeName { get; set; } = "";

        public string Faculty { get; set; } = "";
    }
}