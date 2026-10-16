using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CampusPulse.Student.Services;

public class StudentNotificationsApiService
    : IStudentNotificationsApiService
{
    private readonly HttpClient httpClient;
    private readonly ISessionService sessionService;

    public StudentNotificationsApiService(
        HttpClient httpClient,
        ISessionService sessionService)
    {
        this.httpClient = httpClient;
        this.sessionService = sessionService;
    }

    public async Task<IReadOnlyList<StudentNotification>>
        GetNotificationsAsync()
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/student/notifications");

        using var response = await httpClient.SendAsync(request);

        EnsureAuthorized(response);

        response.EnsureSuccessStatusCode();

        var notifications = await response.Content.ReadFromJsonAsync<
            List<StudentNotification>>();

        return notifications ?? new List<StudentNotification>();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/student/notifications/unread-count");

        using var response = await httpClient.SendAsync(request);

        EnsureAuthorized(response);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<
            UnreadCountResponse>();

        return result?.UnreadCount ?? 0;
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        if (notificationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(notificationId));
        }

        using var request = await CreateRequestAsync(
            HttpMethod.Patch,
            $"api/student/notifications/{notificationId}/read");

        using var response = await httpClient.SendAsync(request);

        EnsureAuthorized(response);

        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string endpoint)
    {
        string? token = await sessionService.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException(
                "Please sign in to view your notifications.");
        }

        var request = new HttpRequestMessage(method, endpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return request;
    }

    private static void EnsureAuthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                "Your session has expired. Please sign in again.");
        }
    }

    private sealed class UnreadCountResponse
    {
        public int UnreadCount { get; set; }
    }
}