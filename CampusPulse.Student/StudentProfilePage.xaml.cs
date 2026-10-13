using CampusPulse.Student.Services;

namespace CampusPulse.Student;

public partial class StudentProfilePage : ContentPage
{
    private readonly ISessionService sessionService;

    public StudentProfilePage()
    {
        InitializeComponent();

        sessionService = IPlatformApplication.Current?
            .Services.GetRequiredService<ISessionService>()
            ?? throw new InvalidOperationException(
                "Session service is unavailable.");
    }

    private async void OnLogoutClicked(
        object? sender,
        EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(
            "Log Out",
            "Are you sure you want to log out of CampusPulse?",
            "Log Out",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        sessionService.ClearToken();

        await Shell.Current.GoToAsync("//MainPage");
    }
}
