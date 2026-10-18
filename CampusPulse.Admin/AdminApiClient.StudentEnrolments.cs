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
        private const string StudentEnrolmentEndpoint =
            "api/admin/student-enrolments/students";

        private static HttpRequestMessage CreateStudentEnrolmentRequest(
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

        public async Task<List<AdminStudentEnrolmentRecord>>
            GetStudentEnrolmentsAsync()
        {
            using var request = CreateStudentEnrolmentRequest(
                HttpMethod.Get,
                StudentEnrolmentEndpoint);

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<
                List<AdminStudentEnrolmentRecord>>()
                ?? new List<AdminStudentEnrolmentRecord>();
        }

        public async Task<AdminStudentEnrolmentDetail>
            GetStudentEnrolmentDetailAsync(int studentId)
        {
            if (studentId <= 0)
            {
                throw new ArgumentException("Invalid student ID.");
            }

            using var request = CreateStudentEnrolmentRequest(
                HttpMethod.Get,
                $"{StudentEnrolmentEndpoint}/{studentId}");

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<
                AdminStudentEnrolmentDetail>()
                ?? throw new InvalidOperationException(
                    "Student enrolment response was empty.");
        }

        public async Task AssignStudentProgrammeAsync(
            int studentId,
            int programmeId)
        {
            if (studentId <= 0 || programmeId <= 0)
            {
                throw new ArgumentException(
                    "A valid student and programme are required.");
            }

            using var request = CreateStudentEnrolmentRequest(
                HttpMethod.Put,
                $"{StudentEnrolmentEndpoint}/{studentId}/programme");

            request.Content = JsonContent.Create(new
            {
                ProgrammeId = programmeId
            });

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }
        }

        public async Task AddStudentModuleAsync(
            int studentId,
            int moduleId)
        {
            if (studentId <= 0 || moduleId <= 0)
            {
                throw new ArgumentException(
                    "A valid student and module are required.");
            }

            using var request = CreateStudentEnrolmentRequest(
                HttpMethod.Post,
                $"{StudentEnrolmentEndpoint}/{studentId}/modules");

            request.Content = JsonContent.Create(new
            {
                ModuleId = moduleId
            });

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }
        }

        public async Task RemoveStudentModuleAsync(
            int studentId,
            int moduleId)
        {
            if (studentId <= 0 || moduleId <= 0)
            {
                throw new ArgumentException(
                    "A valid student and module are required.");
            }

            using var request = CreateStudentEnrolmentRequest(
                HttpMethod.Delete,
                $"{StudentEnrolmentEndpoint}/{studentId}/modules/{moduleId}");

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }
        }
    }

    public sealed record AdminStudentEnrolmentRecord(
        int StudentId,
        string StudentNumber,
        string FirstName,
        string LastName,
        int ProgrammeId,
        string ProgrammeName,
        int YearLevel,
        bool IsActive)
    {
        public string DisplayName =>
            $"{FirstName} {LastName} | {StudentNumber}".Trim();

        public string AccountStatus =>
            IsActive ? "Active" : "Inactive";
    }

    public sealed record AdminStudentEnrolmentDetail(
        int StudentId,
        int ProgrammeId,
        int YearLevel,
        List<AdminStudentEnrolledModule> Modules);

    public sealed record AdminStudentEnrolledModule(
        int ModuleId,
        string ModuleCode,
        string ModuleName,
        int ProgrammeId)
    {
        public string DisplayName =>
            $"{ModuleCode} - {ModuleName}";
    }
}