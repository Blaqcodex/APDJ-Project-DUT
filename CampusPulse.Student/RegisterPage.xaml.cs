
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CampusPulse.Student;

public partial class RegisterPage : ContentPage
{
    private readonly HttpClient httpClient;

    public RegisterPage(IHttpClientFactory httpClientFactory)
    {
        InitializeComponent();

        httpClient = httpClientFactory.CreateClient(
            nameof(RegisterPage));
    }

    private async void OnRegisterClicked(
        object? sender,
        EventArgs e)
    {
        string firstName = FirstNameEntry.Text?.Trim() ?? "";
        string lastName = LastNameEntry.Text?.Trim() ?? "";
        string studentNumber = StudentNumberEntry.Text?.Trim() ?? "";
        string email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? "";
        string recoveryEmail =
            RecoveryEmailEntry.Text?.Trim().ToLowerInvariant() ?? "";
        string password = PasswordEntry.Text ?? "";
        string confirmPassword = ConfirmPasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(studentNumber) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(recoveryEmail) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlertAsync(
                "Missing Details",
                "Please complete all fields.",
                "OK");
            return;
        }

        if (studentNumber.Length != 8 ||
            !studentNumber.All(char.IsDigit))
        {
            await DisplayAlertAsync(
                "Invalid Student Number",
                "Student number must contain exactly 8 digits.",
                "OK");
            return;
        }

        string expectedEmail =
            $"{studentNumber}@dut4life.ac.za";

        if (email != expectedEmail)
        {
            await DisplayAlertAsync(
                "Invalid DUT Email",
                "Your DUT email must match your student number.",
                "OK");
            return;
        }

        if (!Regex.IsMatch(
            recoveryEmail,
            @"^[^@\s]+@gmail\.com$",
            RegexOptions.IgnoreCase))
        {
            await DisplayAlertAsync(
                "Invalid Recovery Email",
                "Please enter a valid personal Gmail address.",
                "OK");
            return;
        }

        if (password.Length < 8)
        {
            await DisplayAlertAsync(
                "Weak Password",
                "Your password must contain at least 8 characters.",
                "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlertAsync(
                "Password Mismatch",
                "Passwords do not match.",
                "OK");
            return;
        }

        RegisterButton.IsEnabled = false;
        RegisterButton.Text = "Creating account...";

        try
        {
            var request = new
            {
                FirstName = firstName,
                LastName = lastName,
                StudentNumber = studentNumber,
                Email = email,
                RecoveryEmail = recoveryEmail,
                Password = password
            };

            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    "api/Auth/register",
                    request);

            if (!response.IsSuccessStatusCode)
            {
                string message =
                    "Registration failed. Please check your details.";

                try
                {
                    using var document = JsonDocument.Parse(
                        await response.Content.ReadAsStringAsync());

                    if (document.RootElement.TryGetProperty(
                        "message", out JsonElement serverMessage))
                    {
                        message = serverMessage.GetString() ?? message;
                    }
                }
                catch (JsonException)
                {
                    // Keep the default error message.
                }

                await DisplayAlertAsync(
                    "Registration Failed",
                    message,
                    "OK");

                return;
            }

            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;

            await DisplayAlertAsync(
                "Account Created",
                "Your account was created successfully. " +
                "Your recovery Gmail still needs verification. " +
                "Please sign in.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync(
                "Connection Error",
                $"Registration request failed:\n{ex.Message}",
                "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync(
                "Connection Timeout",
                "The server took too long to respond.",
                "OK");
        }
        finally
        {
            RegisterButton.IsEnabled = true;
            RegisterButton.Text = "Create Account";
        }
    }

    private async void OnBackToLoginClicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
