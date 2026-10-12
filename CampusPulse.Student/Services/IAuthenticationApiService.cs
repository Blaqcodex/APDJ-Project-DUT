
namespace CampusPulse.Student.Services
{
    public interface IAuthenticationApiService
    {
        Task<PasswordRecoveryResult> ForgotPasswordAsync(string email);

        Task<PasswordRecoveryResult> ResetPasswordAsync(
            string email,
            string token,
            string newPassword);

        Task<LoginResult> LoginAsync(
            string email,
            string password);
    }


    public class PasswordRecoveryResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
    }
    public class LoginResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Token { get; set; }

        public int UserId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Role { get; set; }
    }
}
