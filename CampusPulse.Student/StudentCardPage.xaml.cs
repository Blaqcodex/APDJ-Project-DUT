using CampusPulse.Student.Services;

namespace CampusPulse.Student;

public partial class StudentCardPage : ContentPage
{
    private readonly IStudentProfileApiService profileService;

    public StudentCardPage()
    {
        InitializeComponent();

        profileService = IPlatformApplication.Current?
            .Services.GetRequiredService<IStudentProfileApiService>()
            ?? throw new InvalidOperationException(
                "Student profile service is unavailable.");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        StudentCard.IsVisible = false;
        StatusLabel.Text = "Loading your student details...";

        try
        {
            StudentProfile? profile =
                await profileService.GetProfileAsync();

            if (profile == null)
            {
                StatusLabel.Text =
                    "Unable to load your student details. Please try again later.";
                return;
            }

            StudentNameLabel.Text =
                $"{profile.FirstName} {profile.LastName}".Trim();

            StudentNumberLabel.Text =
                $"Student No: {profile.StudentNumber ?? "Not available"}";

            EmailLabel.Text =
                profile.Email ?? "Email not available";

            InstitutionLabel.Text =
                profile.InstitutionName ?? "Institution not available";

            ProgrammeLabel.Text =
                $"Programme: {profile.ProgrammeName ?? "Not yet assigned"}";

            FacultyLabel.Text =
                $"Faculty: {profile.Faculty ?? "Not yet assigned"}";

            YearLevelLabel.Text =
                profile.YearLevel.HasValue
                    ? $"Year Level: {profile.YearLevel.Value}"
                    : "Year Level: Not yet assigned";

            StudentCard.IsVisible = true;
            StatusLabel.Text = "";
        }
        catch (Exception)
        {
            StatusLabel.Text =
                "Something went wrong while loading your student card.";
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}
