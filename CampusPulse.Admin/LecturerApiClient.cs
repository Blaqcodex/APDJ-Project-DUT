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
            BaseAddress = new System.Uri("http://localhost:5240/"),
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

            return new LecturerLoginResult(
                token,
                $"{firstName} {lastName}".Trim());
        }
    }

    public sealed record LecturerLoginResult(
        string Token,
        string DisplayName);
}