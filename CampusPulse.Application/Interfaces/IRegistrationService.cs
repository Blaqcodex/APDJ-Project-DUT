namespace CampusPulse.Application.Interfaces
{
    public interface IRegistrationService
    {
        bool RegisterStudent(
            string firstName,
            string lastName,
            string email,
            string studentNumber,
            string password
        );
    }
}