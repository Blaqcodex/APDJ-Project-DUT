using Microsoft.Maui.ApplicationModel.Communication;

namespace CampusPulse.Student;

public partial class StudentHelpSupportPage : ContentPage
{
    public StudentHelpSupportPage()
    {
        InitializeComponent();
    }

    private async void OnContactSupportClicked(
        object? sender,
        EventArgs e)
    {
        string address = SupportEmailEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(address) ||
            !System.Net.Mail.MailAddress.TryCreate(address, out var parsed) ||
            parsed.Address != address)
        {
            await DisplayAlertAsync(
                "Email Address Required",
                "Enter a valid support email address provided by your institution.",
                "OK");
            return;
        }

        try
        {
            var message = new EmailMessage
            {
                Subject = "CampusPulse Student Support Request",
                Body = "Hello,\n\nI need help with CampusPulse.\n\nIssue:\n\n",
                To = new List<string> { address }
            };

            await Email.Default.ComposeAsync(message);
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Email App Unavailable",
                "No compatible email app could be opened. Check your email app setup or contact your institution directly.",
                "OK");
        }
    }
}
