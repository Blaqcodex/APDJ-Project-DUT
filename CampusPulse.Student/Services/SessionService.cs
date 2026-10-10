
using Microsoft.Maui.Storage;

namespace CampusPulse.Student.Services
{
    public class SessionService : ISessionService
    {
        private const string TokenKey = "campuspulse_student_token";

        public async Task SaveTokenAsync(string token)
        {
            await SecureStorage.Default.SetAsync(
                TokenKey,
                token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }

        public void ClearToken()
        {
            SecureStorage.Default.Remove(TokenKey);
        }
    }
}
