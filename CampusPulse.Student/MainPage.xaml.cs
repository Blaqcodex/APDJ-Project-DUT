
using CampusPulse.Student.Services;

namespace CampusPulse.Student
{
    public partial class MainPage : ContentPage
    {
        private readonly IAuthenticationApiService authenticationService;

        public MainPage(IAuthenticationApiService authenticationService)
        {
            InitializeComponent();

            this.authenticationService = authenticationService;
        }

        private async void OnSignInClicked(
            object? sender,
            EventArgs e)
        {
            string email = EmailEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text ?? "";

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlertAsync(
                    "Missing Details",
                    "Please enter your email and password.",
                    "OK");

                return;
            }

            SignInButton.IsEnabled = false;
            SignInButton.Text = "Signing in...";

            try
            {
                LoginResult result =
                    await authenticationService.LoginAsync(
                        email,
                        password);

                if (!result.Success)
                {
                    await DisplayAlertAsync(
                        "Login Failed",
                        result.Message,
                        "OK");

                    return;
                }

                if (result.Role != "Student")
                {
                    await DisplayAlertAsync(
                        "Access Denied",
                        "This application is for students only.",
                        "OK");

                    return;
                }

                await DisplayAlertAsync(
                    "Welcome!",
                    $"Login successful. Welcome {result.FirstName}!",
                    "OK");

                // Next milestone:
                // Store the JWT securely and navigate to the dashboard.
            }
            finally
            {
                SignInButton.IsEnabled = true;
                SignInButton.Text = "Sign In  →";
            }
        }
    }
}
