using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CampusPulse.Student.Services
{
    public class StudentProfileApiService : IStudentProfileApiService
    {
        private readonly HttpClient httpClient;
        private readonly ISessionService sessionService;

        public StudentProfileApiService(
            HttpClient httpClient,
            ISessionService sessionService)
        {
            this.httpClient = httpClient;
            this.sessionService = sessionService;
        }

        public async Task<StudentProfile?> GetProfileAsync()
        {
            string? token = await sessionService.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    "api/Student/profile");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                using HttpResponseMessage response =
                    await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content
                    .ReadFromJsonAsync<StudentProfile>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }
}
