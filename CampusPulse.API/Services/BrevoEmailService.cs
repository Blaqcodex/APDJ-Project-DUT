
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CampusPulse.API.Services
{
    public class BrevoEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public BrevoEmailService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string message)
        {
            string apiKey =
                _configuration["Brevo:ApiKey"]
                ?? throw new InvalidOperationException(
                    "Brevo API key is not configured.");

            string senderEmail =
                _configuration["Brevo:SenderEmail"]
                ?? throw new InvalidOperationException(
                    "Brevo sender email is not configured.");

            string senderName =
                _configuration["Brevo:SenderName"]
                ?? "CampusPulse";

            var emailRequest = new
            {
                sender = new
                {
                    name = senderName,
                    email = senderEmail
                },
                to = new[]
                {
                    new { email = recipientEmail }
                },
                subject = subject,
                textContent = message
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email");

            request.Headers.Add("api-key", apiKey);
            request.Content = JsonContent.Create(emailRequest);

            using HttpResponseMessage response =
                await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Brevo email delivery request failed. " +
                    $"HTTP status: {(int)response.StatusCode}");
            }
        }
    }
}
