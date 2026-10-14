namespace CampusPulse.Student.Services
{
    public interface IStudentProfileApiService
    {
        Task<StudentProfile?> GetProfileAsync();
    }
}
