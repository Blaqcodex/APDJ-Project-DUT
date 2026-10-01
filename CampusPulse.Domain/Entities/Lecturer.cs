namespace CampusPulse.Domain.Entities
{
    public class Lecturer
    {
        public int LecturerId { get; set; }

        public int UserId { get; set; }

        public string StaffNumber { get; set; } = "";

        public string Department { get; set; } = "";
    }
}