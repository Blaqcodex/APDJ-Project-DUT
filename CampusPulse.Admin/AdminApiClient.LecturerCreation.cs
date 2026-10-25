using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CampusPulse.Admin
{
    public sealed partial class AdminApiClient
    {
        public async Task CreateLecturerAsync(
            string firstName,
            string lastName,
            string email,
            string staffNumber,
            string department,
            string password)
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/admin/lecturers");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    AdminSession.Token);

            request.Content = JsonContent.Create(new
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                StaffNumber = staffNumber,
                Department = department,
                Password = password
            });

            using var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AdminApiException(response.StatusCode);
            }
        }
    }
}
