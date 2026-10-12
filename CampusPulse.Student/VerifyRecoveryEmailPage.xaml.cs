using CampusPulse.Student.Services;

namespace CampusPulse.Student;

public partial class VerifyRecoveryEmailPage : ContentPage
{
    private readonly IRecoveryEmailApiService _recoveryEmailService;
    private bool _isBusy;

    public VerifyRecoveryEmailPage(
        IRecoveryEmailApiService recoveryEmailService)
    {
        InitializeComponent();
        _recoveryEmailService = recoveryEmailService;
    }

    private async void OnSendCodeClicked(
        object? sender,
        EventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        SetBusy(true);
        ShowStatus("Sending verification code...", false);

        try
        {
            RecoveryEmailResult result =
                await _recoveryEmailService.SendCodeAsync();

            ShowStatus(result.Message, result.Success);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnVerifyCodeClicked(
        object? sender,
        EventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        string code = VerificationCodeEntry.Text?.Trim()
            ?? string.Empty;

        if (code.Length != 6 ||
            !code.All(char.IsAsciiDigit))
        {
            ShowStatus(
                "Enter the six-digit code from your recovery email.",
                false);

            return;
        }

        SetBusy(true);
        ShowStatus("Verifying your code...", false);

        try
        {
            RecoveryEmailResult result =
                await _recoveryEmailService.VerifyCodeAsync(code);

            ShowStatus(result.Message, result.Success);

            if (result.Success)
            {
                VerificationCodeEntry.Text = string.Empty;
                SendCodeButton.IsEnabled = false;
                VerifyCodeButton.IsEnabled = false;
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;

        SendCodeButton.IsEnabled = !busy;
        VerifyCodeButton.IsEnabled = !busy;
    }

    private void ShowStatus(string message, bool success)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = success
            ? Color.FromArgb("#69E5E3")
            : Color.FromArgb("#FFAD83");

        StatusLabel.IsVisible = true;
    }
}
