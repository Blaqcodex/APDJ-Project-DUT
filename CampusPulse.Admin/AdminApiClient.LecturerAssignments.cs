using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CampusPulse.Admin
{
    public sealed partial class AdminApiClient
    {
        private const string AssignmentEndpoint =
            "api/admin/lecturer-assignments";

        private static HttpRequestMessage CreateAssignmentRequest(
            HttpMethod method,
            string endpoint)
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            var request = new HttpRequestMessage(method, endpoint);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    AdminSession.Token);

            return request;
        }

        public async Task<List<AdminLecturerRecord>> GetLecturersAsync()
        {
            using var request = CreateAssignmentRequest(
                HttpMethod.Get,
                AssignmentEndpoint + "/lecturers");

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<
                List<AdminLecturerRecord>>()
                ?? new List<AdminLecturerRecord>();
        }

        public async Task<List<AdminLecturerAssignmentRecord>>
            GetLecturerAssignmentsAsync()
        {
            using var request = CreateAssignmentRequest(
                HttpMethod.Get,
                AssignmentEndpoint);

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<
                List<AdminLecturerAssignmentRecord>>()
                ?? new List<AdminLecturerAssignmentRecord>();
        }

        public async Task AssignLecturerAsync(
            int lecturerId,
            int moduleId)
        {
            if (lecturerId <= 0 || moduleId <= 0)
            {
                throw new ArgumentException(
                    "A valid lecturer and module are required.");
            }

            using var request = CreateAssignmentRequest(
                HttpMethod.Post,
                AssignmentEndpoint);

            request.Content = JsonContent.Create(new
            {
                LecturerId = lecturerId,
                ModuleId = moduleId
            });

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }
        }

        public async Task RemoveLecturerAssignmentAsync(
            int lecturerModuleId)
        {
            if (lecturerModuleId <= 0)
            {
                throw new ArgumentException(
                    "A valid assignment is required.");
            }

            using var request = CreateAssignmentRequest(
                HttpMethod.Delete,
                AssignmentEndpoint + "/" + lecturerModuleId);

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }
        }
    }

    public sealed record AdminLecturerRecord(
        int LecturerId,
        string StaffNumber,
        string Department,
        string FirstName,
        string LastName,
        string Email)
    {
        public string DisplayName =>
            $"{FirstName} {LastName} | {StaffNumber}".Trim();
    }

    public sealed record AdminLecturerAssignmentRecord(
        int LecturerModuleId,
        int LecturerId,
        int ModuleId,
        string ModuleCode,
        string ModuleName,
        string StaffNumber,
        string FirstName,
        string LastName,
        string Email,
        bool IsActive)
    {
        public string LecturerName =>
            $"{FirstName} {LastName}".Trim();

        public string ModuleDisplay =>
            $"{ModuleCode} - {ModuleName}";

        public string AccountStatus =>
            IsActive ? "Active" : "Inactive";
    }
}
