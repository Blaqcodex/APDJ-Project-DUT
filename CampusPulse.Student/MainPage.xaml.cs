
using CampusPulse.Student.Services;

namespace CampusPulse.Student
{
    public partial class MainPage : ContentPage
    {
        private readonly IAuthenticationApiService authenticationService;
        private readonly ISessionService sessionService;

        public MainPage(
            IAuthenticationApiService authenticationService,
            ISessionService sessionService)
        {
            InitializeComponent();

            this.authenticationService = authenticationService;
            this.sessionService = sessionService;
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

                if (result.Role != "Student" ||
                    string.IsNullOrWhiteSpace(result.Token))
                {
                    await DisplayAlertAsync(
                        "Access Denied",
                        "A valid student session could not be created.",
                        "OK");

                    return;
                }

                try
                {
                    await sessionService.SaveTokenAsync(result.Token);

                    string? savedToken =
                        await sessionService.GetTokenAsync();

                    if (string.IsNullOrWhiteSpace(savedToken) ||
                        savedToken != result.Token)
                    {
                        await DisplayAlertAsync(
                            "Session Error",
                            "The authentication session could not be verified.",
                            "OK");

                        return;
                    }
                }
                catch (Exception)
                {
                    await DisplayAlertAsync(
                        "Session Error",
                        "Your session could not be saved. Please try again.",
                        "OK");

                    return;
                }

                PasswordEntry.Text = string.Empty;

                await DisplayAlertAsync(
                    "Welcome!",
                    $"Login successful. Welcome {result.FirstName}!",
                    "OK");

                await Shell.Current.GoToAsync(
                    nameof(StudentDashboardPage));
            }
            finally
            {
                SignInButton.IsEnabled = true;
                SignInButton.Text = "Sign In  →";
            }
        }

        private async void OnCreateAccountClicked(
            object? sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync(
                nameof(RegisterPage));
        }
    }
}
