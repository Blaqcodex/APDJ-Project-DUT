using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CampusPulse.Admin
{
    public sealed partial class AdminApiClient
    {
        private const string LecturerProvisioningEndpoint =
            "api/admin/lecturers";

        public async Task<AdminCreatedLecturerRecord> CreateLecturerAsync(
            AdminCreateLecturerRequest lecturer)
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            if (lecturer == null)
            {
                throw new ArgumentNullException(nameof(lecturer));
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                LecturerProvisioningEndpoint);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    AdminSession.Token);

            request.Content = JsonContent.Create(lecturer);

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<
                AdminCreatedLecturerRecord>()
                ?? throw new InvalidOperationException(
                    "Lecturer provisioning response was empty.");
        }
    }

    public sealed record AdminCreateLecturerRequest(
        string FirstName,
        string LastName,
        string Email,
        string StaffNumber,
        string Department,
        string TemporaryPassword);

    public sealed record AdminCreatedLecturerRecord(
        int LecturerId,
        int UserId,
        string FirstName,
        string LastName,
        string Email,
        string StaffNumber,
        string Department,
        string Role,
        bool IsActive,
        bool MustChangePassword,
        string Message);
}