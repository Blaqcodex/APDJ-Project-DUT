using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace CampusPulse.Admin
{
    public sealed class AdminApiClient
    {
        private static readonly HttpClient Http = new()
        {
            BaseAddress = new Uri("http://localhost:5240/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        public async Task<AdminLoginResult> LoginAsync(
            string email,
            string password)
        {
            using var response = await Http.PostAsJsonAsync(
                "api/admin/auth/login",
                new { Email = email, Password = password });

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    response.StatusCode == HttpStatusCode.Unauthorized
                        ? "Invalid administrator credentials."
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
                    "Admin",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "This account is not authorized for the Admin Portal.");
            }

            return new AdminLoginResult(
                token,
                $"{firstName} {lastName}".Trim());
        }
        public async Task<System.Collections.Generic.List<AdminModuleRecord>> GetModulesAsync()
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get, "api/admin/modules");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", AdminSession.Token);

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }

            var modules = await response.Content.ReadFromJsonAsync<
                System.Collections.Generic.List<AdminModuleRecord>>();

            return modules ?? new System.Collections.Generic.List<AdminModuleRecord>();
        }

        public async Task CreateModuleAsync(
            string moduleCode,
            string moduleName,
            int programmeId)
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post, "api/admin/modules");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", AdminSession.Token);

            request.Content = JsonContent.Create(new
            {
                ModuleCode = moduleCode,
                ModuleName = moduleName,
                ProgrammeId = programmeId
            });

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }
        }
        public async Task<System.Collections.Generic.List<AdminUserRecord>> GetUsersAsync(
            string? role = null)
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            string endpoint = "api/admin/users";

            if (!string.IsNullOrWhiteSpace(role))
            {
                endpoint += "?role=" + Uri.EscapeDataString(role);
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get, endpoint);

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", AdminSession.Token);

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }

            var users = await response.Content.ReadFromJsonAsync<
                System.Collections.Generic.List<AdminUserRecord>>();

            return users ?? new System.Collections.Generic.List<AdminUserRecord>();
        }
    }

    public sealed record AdminModuleRecord(
        int ModuleId,
        string ModuleCode,
        string ModuleName,
        int ProgrammeId,
        string ProgrammeName,
        string Faculty);
    public sealed record AdminLoginResult(
        string Token,
        string DisplayName);

    public sealed record AdminUserRecord(
        int UserId,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        DateTime CreatedAt);

    public sealed class AdminApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public AdminApiException(HttpStatusCode statusCode)
            : base("Administrator API request failed.")
        {
            StatusCode = statusCode;
        }
    }}