namespace CampusPulse.Student;

public partial class StudentDashboardPage : ContentPage
{
    public StudentDashboardPage()
    {
        InitializeComponent();
    }

    private async void OnTimetableTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            "//StudentTabs/StudentSchedule");
    }

    private async void OnAttendanceTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(StudentAttendancePage));
    }

    private async void OnScanQrTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            "//StudentTabs/StudentScan");
    }

    private async void OnVerifyEmailTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(VerifyRecoveryEmailPage));
    }

    private async void OnStudentCardTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(StudentCardPage));
    }
    private async void OnNoticesTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            "//StudentTabs/StudentAlerts");
    }
}
