namespace CampusPulse.Student.Services
{
    public interface IRecoveryEmailApiService
    {
        Task<RecoveryEmailResult> SendCodeAsync();

        Task<RecoveryEmailResult> VerifyCodeAsync(string code);
    }

    public class RecoveryEmailResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
