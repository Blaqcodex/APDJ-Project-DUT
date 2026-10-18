using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace CampusPulse.Admin
{
    public sealed class LecturerApiClient
    {
        private static readonly HttpClient Http = new()
        {
            BaseAddress = App.ApiBaseAddress,
            Timeout = System.TimeSpan.FromSeconds(15)
        };

        public async Task<LecturerLoginResult> LoginAsync(
            string email,
            string password)
        {
            using var response = await Http.PostAsJsonAsync(
                "api/lecturer/auth/login",
                new
                {
                    Email = email,
                    Password = password
                });

            if (!response.IsSuccessStatusCode)
            {
                throw new System.InvalidOperationException(
                    response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized
                        ? "Invalid lecturer email or password."
                        : "Unable to sign in. Please try again.");
            }

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

            var root = json.RootElement;

            string token = root.GetProperty("token").GetString() ?? "";
            string firstName = root.GetProperty("firstName").GetString() ?? "";
            string lastName = root.GetProperty("lastName").GetString() ?? "";
            string role = root.GetProperty("role").GetString() ?? "";

            if (string.IsNullOrWhiteSpace(token) ||
                !string.Equals(
                    role,
                    "Lecturer",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.InvalidOperationException(
                    "The account is not authorized for the lecturer portal.");
            }

            bool mustChangePassword = root.GetProperty(
                "mustChangePassword").GetBoolean();

            return new LecturerLoginResult(
                token,
                $"{firstName} {lastName}".Trim(),
                mustChangePassword);
        }
        public async Task ChangePasswordAsync(
            string token,
            string currentPassword,
            string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new System.InvalidOperationException(
                    "Lecturer session is missing. Please sign in again.");
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                throw new System.InvalidOperationException(
                    "Enter your current password.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) ||
                newPassword.Length < 8)
            {
                throw new System.InvalidOperationException(
                    "New password must contain at least 8 characters.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/lecturer/auth/change-password");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    token);

            request.Content = JsonContent.Create(new
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });

            using var response = await Http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                throw new System.InvalidOperationException(
                    "Your lecturer session has expired. Please sign in again.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                throw new System.InvalidOperationException(
                    "This account is not authorized to change the password.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.BadRequest)
            {
                using var json = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync());

                if (json.RootElement.TryGetProperty(
                    "message",
                    out var message))
                {
                    throw new System.InvalidOperationException(
                        message.GetString() ??
                        "Password change was rejected.");
                }

                throw new System.InvalidOperationException(
                    "Check your current and new passwords.");
            }

            throw new System.InvalidOperationException(
                "Unable to change password. Please try again.");
        }
    }

    public sealed record LecturerLoginResult(
        string Token,
        string DisplayName,
        bool MustChangePassword);
}