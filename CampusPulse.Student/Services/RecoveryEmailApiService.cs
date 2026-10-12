using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CampusPulse.Student.Services
{
    public class RecoveryEmailApiService : IRecoveryEmailApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ISessionService _sessionService;

        public RecoveryEmailApiService(
            HttpClient httpClient,
            ISessionService sessionService)
        {
            _httpClient = httpClient;
            _sessionService = sessionService;
        }

        public async Task<RecoveryEmailResult> SendCodeAsync()
        {
            return await SendRequestAsync(
                "api/recovery-email/send-code",
                new { });
        }

        public async Task<RecoveryEmailResult> VerifyCodeAsync(string code)
        {
            return await SendRequestAsync(
                "api/recovery-email/verify-code",
                new { Code = code.Trim() });
        }

        private async Task<RecoveryEmailResult> SendRequestAsync(
            string endpoint,
            object body)
        {
            string? token = await _sessionService.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                return new RecoveryEmailResult
                {
                    Success = false,
                    Message = "Your session has expired. Please log in again."
                };
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    endpoint);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Content = JsonContent.Create(body);

                using HttpResponseMessage response =
                    await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return new RecoveryEmailResult
                    {
                        Success = true,
                        Message = endpoint.EndsWith("send-code")
                            ? "Verification code sent. Check your recovery Gmail."
                            : "Recovery email verified successfully."
                    };
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new RecoveryEmailResult
                    {
                        Success = false,
                        Message = "Your session has expired. Please log in again."
                    };
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    return new RecoveryEmailResult
                    {
                        Success = false,
                        Message = "Too many requests. Please wait before trying again."
                    };
                }

                return new RecoveryEmailResult
                {
                    Success = false,
                    Message = endpoint.EndsWith("send-code")
                        ? "Unable to send a code. Your email may already be verified, or you may need to wait before requesting another code."
                        : "Invalid or expired verification code."
                };
            }
            catch (HttpRequestException)
            {
                return new RecoveryEmailResult
                {
                    Success = false,
                    Message = "Cannot connect to CampusPulse. Check your connection and make sure the API is running."
                };
            }
            catch (TaskCanceledException)
            {
                return new RecoveryEmailResult
                {
                    Success = false,
                    Message = "The request timed out. Please try again."
                };
            }
            catch (Exception)
            {
                return new RecoveryEmailResult
                {
                    Success = false,
                    Message = "Something went wrong. Please try again."
                };
            }
        }
    }
}
