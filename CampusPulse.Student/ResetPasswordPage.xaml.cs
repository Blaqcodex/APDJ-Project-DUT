using CampusPulse.Student.Services;

namespace CampusPulse.Student;

public partial class ResetPasswordPage : ContentPage, IQueryAttributable
{
    private readonly IAuthenticationApiService authenticationService;
    private string studentEmail = "";
    private bool isBusy;

    public ResetPasswordPage(
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
            studentEmail = email.Trim();
        }
    }

    private async void OnResetPasswordClicked(
        object? sender,
        EventArgs e)
    {
        if (isBusy)
        {
            return;
        }

        string code = ResetCodeEntry.Text?.Trim() ?? "";
        string newPassword = NewPasswordEntry.Text ?? "";
        string confirmPassword = ConfirmPasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(studentEmail))
        {
            ShowStatus("Student email is missing. Please request a new code.", false);
            return;
        }

        if (code.Length != 6 || !code.All(char.IsAsciiDigit))
        {
            ShowStatus("Please enter a valid six-digit reset code.", false);
            return;
        }

        if (newPassword.Length < 8 || newPassword.Length > 100)
        {
            ShowStatus("Password must contain 8 to 100 characters.", false);
            return;
        }

        if (newPassword != confirmPassword)
        {
            ShowStatus("Passwords do not match.", false);
            return;
        }

        isBusy = true;
        ResetPasswordButton.IsEnabled = false;
        ResetPasswordButton.Text = "Resetting...";
        StatusLabel.Text = "";

        try
        {
            PasswordRecoveryResult result =
                await authenticationService.ResetPasswordAsync(
                    studentEmail,
                    code,
                    newPassword);

            ShowStatus(result.Message, result.Success);

            if (result.Success)
            {
                ResetCodeEntry.Text = "";
                NewPasswordEntry.Text = "";
                ConfirmPasswordEntry.Text = "";

                await Shell.Current.GoToAsync("../..");
            }
        }
        catch (Exception)
        {
            ShowStatus("Something went wrong. Please try again.", false);
        }
        finally
        {
            isBusy = false;
            ResetPasswordButton.IsEnabled = true;
            ResetPasswordButton.Text = "Reset Password";
        }
    }

    private void ShowStatus(string message, bool success)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = success
            ? Color.FromArgb("#69E5E3")
            : Color.FromArgb("#FF756B");
    }

    private async void OnBackToLoginClicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("../..");
    }
}