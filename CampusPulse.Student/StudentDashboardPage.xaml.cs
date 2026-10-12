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
            nameof(StudentTimetablePage));
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
            nameof(StudentQrScannerPage));
    }

    private async void OnVerifyEmailTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(VerifyRecoveryEmailPage));
    }
}




