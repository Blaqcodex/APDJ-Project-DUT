using CampusPulse.Student.Services;

namespace CampusPulse.Student
{
    public partial class ForgotPasswordPage : ContentPage, IQueryAttributable
    {
        private readonly IAuthenticationApiService authenticationService;

        public ForgotPasswordPage(
            IAuthenticationApiService authenticationService)
        {
            InitializeComponent();
            this.authenticationService = authenticationService;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue("DutEmail", out object? value) &&
                value is string email)
            {
                RecoveryEmailEntry.Text = email;
            }
        }
        private async void OnSendResetClicked(
            object? sender,
            EventArgs e)
        {
            string email = RecoveryEmailEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email) ||
                !System.Net.Mail.MailAddress.TryCreate(
                    email, out var address) ||
                address.Address != email)
            {
                StatusLabel.TextColor = Color.FromArgb("#FF756B");
                StatusLabel.Text = "Please enter a valid email address.";
                return;
            }

            SendResetButton.IsEnabled = false;
            SendResetButton.Text = "Sending...";
            StatusLabel.Text = "";

            try
            {
                PasswordRecoveryResult result =
                    await authenticationService.ForgotPasswordAsync(email);

                StatusLabel.TextColor = result.Success
                    ? Color.FromArgb("#69E5E3")
                    : Color.FromArgb("#FF756B");

                StatusLabel.Text = result.Message;

                if (result.Success)
                {
                    await Shell.Current.GoToAsync(
                        nameof(ResetPasswordPage),
                        new Dictionary<string, object>
                        {
                            { "DutEmail", email }
                        });
                }
            }
            catch (Exception)
            {
                StatusLabel.TextColor = Color.FromArgb("#FF756B");
                StatusLabel.Text =
                    "Something went wrong. Please try again.";
            }
            finally
            {
                SendResetButton.IsEnabled = true;
                SendResetButton.Text = "Send Reset Instructions";
            }
        }

        private async void OnBackToLoginClicked(
            object? sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
