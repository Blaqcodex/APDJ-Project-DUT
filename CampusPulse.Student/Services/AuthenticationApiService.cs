
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusPulse.Student.Services
{
    public class AuthenticationApiService : IAuthenticationApiService
    {
        private readonly HttpClient httpClient;

        public AuthenticationApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<PasswordRecoveryResult> ForgotPasswordAsync(
            string email)
        {
            const string confirmationMessage =
                "If this email is registered and verified, password reset instructions will be sent.";

            try
            {
                var request = new
                {
                    Email = email.Trim()
                };

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/Auth/forgot-password",
                        request);

                if (response.IsSuccessStatusCode)
                {
                    return new PasswordRecoveryResult
                    {
                        Success = true,
                        Message = confirmationMessage
                    };
                }

                if (response.StatusCode ==
                    (HttpStatusCode)429)
                {
                    return new PasswordRecoveryResult
                    {
                        Success = false,
                        Message = "Too many requests. Please try again later."
                    };
                }

                return new PasswordRecoveryResult
                {
                    Success = false,
                    Message = "Unable to process your request. Please try again."
                };
            }
            catch (HttpRequestException)
            {
                return new PasswordRecoveryResult
                {
                    Success = false,
                    Message = "Cannot connect to CampusPulse. Check your connection."
                };
            }
            catch (TaskCanceledException)
            {
                return new PasswordRecoveryResult
                {
                    Success = false,
                    Message = "The connection timed out. Please try again."
                };
            }
        }

        public async Task<PasswordRecoveryResult> ResetPasswordAsync(
            string email,
            string token,
            string newPassword)
        {
            try
            {
                var request = new
                {
                    Email = email.Trim(),
                    Token = token.Trim(),
                    NewPassword = newPassword
                };

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/Auth/reset-password",
                        request);

                if (response.IsSuccessStatusCode)
                {
                    return new PasswordRecoveryResult
                    {
                        Success = true,
                        Message = "Password updated successfully. You can now log in."
                    };
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    return new PasswordRecoveryResult
                    {
                        Success = false,
                        Message = "Invalid or expired reset code. Please request a new code."
                    };
                }

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    return new PasswordRecoveryResult
                    {
                        Success = false,
                        Message = "Too many attempts. Please try again later."
                    };
                }

                return new PasswordRecoveryResult
                {
                    Success = false,
                    Message = "Unable to reset your password. Please try again."
                };
            }
            catch (HttpRequestException)
            {
                return new PasswordRecoveryResult
                {
                    Success = false,
                    Message = "Cannot connect to CampusPulse. Check your connection."
                };
            }
            catch (TaskCanceledException)
            {
                return new PasswordRecoveryResult
                {
                    Success = false,
                    Message = "The connection timed out. Please try again."
                };
            }
        }
        public async Task<LoginResult> LoginAsync(
            string email,
            string password)
        {
            try
            {
                var request = new
                {
                    Email = email.Trim(),
                    Password = password
                };

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/Auth/login",
                        request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Login failed. Please try again."
                    };
                }

                using var stream =
                    await response.Content.ReadAsStreamAsync();

                using var document =
                    await JsonDocument.ParseAsync(stream);

                JsonElement result = document.RootElement;

                string? token = result.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "The server did not return a login token."
                    };
                }

                return new LoginResult
                {
                    Success = true,
                    Message = result.GetProperty("message").GetString()
                        ?? "Login successful.",
                    Token = token,
                    UserId = result.GetProperty("userId").GetInt32(),
                    FirstName = result.GetProperty("firstName").GetString(),
                    LastName = result.GetProperty("lastName").GetString(),
                    Role = result.GetProperty("role").GetString()
                };
            }
            catch (HttpRequestException)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Cannot connect to CampusPulse. Check your connection."
                };
            }
            catch (TaskCanceledException)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "The connection timed out. Please try again."
                };
            }
            catch (JsonException)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "The server returned an invalid response."
                };
            }
        }
    }
}
