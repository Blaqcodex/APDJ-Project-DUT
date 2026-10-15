using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusPulse.Student.Services;

public class StudentAttendanceApiService : IStudentAttendanceApiService
{
    private readonly HttpClient httpClient;
    private readonly ISessionService sessionService;

    public StudentAttendanceApiService(
        HttpClient httpClient,
        ISessionService sessionService)
    {
        this.httpClient = httpClient;
        this.sessionService = sessionService;
    }

    public async Task<AttendanceCheckInResult> CheckInAsync(string qrToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken) ||
            qrToken.Length > 4096)
        {
            return new(false, "Invalid attendance QR code.");
        }

        string? token;

        try
        {
            token = await sessionService.GetTokenAsync();
        }
        catch (Exception ex) when (
            ex is not OutOfMemoryException)
        {
            return new(false,
                "Unable to access your login session. Please sign in again.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return new(false, "Please sign in before scanning attendance.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/attendance/check-in");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Content = JsonContent.Create(new
            {
                qrToken
            });

            using var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return new(true, "Attendance recorded successfully.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new(false,
                    "Your login session has expired. Please sign in again.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new(false,
                    "Your account is not authorized for attendance.");
            }

            string? serverMessage = null;

            try
            {
                using var json = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync());

                if (json.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (json.RootElement.TryGetProperty(
                        "message", out var message) &&
                        message.ValueKind == JsonValueKind.String)
                    {
                        serverMessage = message.GetString();
                    }
                    else if (json.RootElement.TryGetProperty(
                        "title", out var title) &&
                        title.ValueKind == JsonValueKind.String)
                    {
                        serverMessage = title.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Fall back to an HTTP status-based message.
            }

            if (!string.IsNullOrWhiteSpace(serverMessage))
            {
                return new(false, serverMessage);
            }

            return new(false, response.StatusCode switch
            {
                HttpStatusCode.Conflict =>
                    "Attendance has already been recorded.",
                HttpStatusCode.BadRequest =>
                    "Invalid or expired attendance QR code.",
                HttpStatusCode.NotFound =>
                    "Attendance session was not found.",
                _ => "Attendance could not be recorded. Please try again."
            });
        }
        catch (HttpRequestException)
        {
            return new(false,
                "Cannot connect to CampusPulse. Check your connection.");
        }
        catch (TaskCanceledException)
        {
            return new(false,
                "Attendance request timed out. Please try again.");
        }
    }
}