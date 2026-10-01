namespace CampusPulse.Domain.Entities
{
    public class Student
    {
        public int StudentId { get; set; }

        public int UserId { get; set; }

        public string StudentNumber { get; set; } = "";

        public int ProgrammeId { get; set; }

        public int YearLevel { get; set; }
    }
}