using CampusPulse.Student.Services;

namespace CampusPulse.Student;

public partial class StudentProfilePage : ContentPage
{
    private readonly ISessionService sessionService;
    private readonly IStudentProfileApiService profileService;

    public StudentProfilePage()
    {
        InitializeComponent();

        profileService = IPlatformApplication.Current?
            .Services.GetRequiredService<IStudentProfileApiService>()
            ?? throw new InvalidOperationException(
                "Student profile service is unavailable.");
        sessionService = IPlatformApplication.Current?
            .Services.GetRequiredService<ISessionService>()
            ?? throw new InvalidOperationException(
                "Session service is unavailable.");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        ProfileLoadingIndicator.IsVisible = true;
        ProfileLoadingIndicator.IsRunning = true;
        ProfileStatusLabel.Text = "Loading your student profile...";

        try
        {
            StudentProfile? profile = await profileService.GetProfileAsync();

            if (profile is null)
            {
                ProfileStatusLabel.Text =
                    "Unable to load your profile. Please try again later.";
                return;
            }

            string fullName =
                $"{profile.FirstName} {profile.LastName}".Trim();

            ProfileNameLabel.Text =
                string.IsNullOrWhiteSpace(fullName)
                    ? "Student"
                    : fullName;

            ProfileNumberLabel.Text =
                $"Student No: {profile.StudentNumber ?? "Not available"}";

            ProfileEmailLabel.Text =
                profile.Email ?? "Email not available";

            ProfileInstitutionLabel.Text =
                profile.InstitutionName ?? "Institution not available";

            ProfileProgrammeLabel.Text =
                $"Programme: {profile.ProgrammeName ?? "Not yet assigned"}";

            ProfileFacultyLabel.Text =
                $"Faculty: {profile.Faculty ?? "Not yet assigned"}";

            ProfileYearLabel.Text =
                profile.YearLevel.HasValue && profile.YearLevel.Value > 0
                    ? $"Year Level: {profile.YearLevel.Value}"
                    : "Year Level: Not yet assigned";

            ProfileStatusLabel.Text = "";
        }
        catch (Exception)
        {
            ProfileStatusLabel.Text =
                "Something went wrong while loading your profile.";
        }
        finally
        {
            ProfileLoadingIndicator.IsRunning = false;
            ProfileLoadingIndicator.IsVisible = false;
        }
    }
    private async void OnHelpSupportTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(StudentHelpSupportPage));
    }
    private async void OnSettingsTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(StudentSettingsPage));
    }
    private async void OnStudentCardTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(StudentCardPage));
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
