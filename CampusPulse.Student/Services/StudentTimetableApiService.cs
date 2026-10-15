using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CampusPulse.Student.Services;

public class StudentTimetableApiService : IStudentTimetableApiService
{
    private readonly HttpClient httpClient;
    private readonly ISessionService sessionService;

    public StudentTimetableApiService(
        HttpClient httpClient,
        ISessionService sessionService)
    {
        this.httpClient = httpClient;
        this.sessionService = sessionService;
    }

    public async Task<IReadOnlyList<StudentTimetableEntry>>
        GetTimetableAsync()
    {
        string? token = await sessionService.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException(
                "Please sign in to view your timetable.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/student/timetable");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                "Your session has expired. Please sign in again.");
        }

        response.EnsureSuccessStatusCode();

        var entries = await response.Content.ReadFromJsonAsync<
            List<StudentTimetableEntry>>();

        return entries ?? new List<StudentTimetableEntry>();
    }
}