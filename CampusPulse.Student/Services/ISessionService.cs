
namespace CampusPulse.Student.Services
{
    public interface ISessionService
    {
        Task SaveTokenAsync(string token);

        Task<string?> GetTokenAsync();

        void ClearToken();
    }
}
