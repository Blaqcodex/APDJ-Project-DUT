using CampusPulse.Student.Services;

namespace CampusPulse.Student;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(VerifyRecoveryEmailPage), typeof(VerifyRecoveryEmailPage));
        Routing.RegisterRoute(nameof(ResetPasswordPage), typeof(ResetPasswordPage));
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
        Routing.RegisterRoute(nameof(StudentQrScannerPage), typeof(StudentQrScannerPage));
        Routing.RegisterRoute(nameof(StudentDashboardPage), typeof(StudentDashboardPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(StudentTimetablePage), typeof(StudentTimetablePage));
        Routing.RegisterRoute(nameof(StudentAttendancePage), typeof(StudentAttendancePage));

        Routing.RegisterRoute(nameof(StudentCardPage), typeof(StudentCardPage));
        Routing.RegisterRoute(nameof(StudentSettingsPage), typeof(StudentSettingsPage));

        Navigating += OnShellNavigating;
    }

    private async void OnShellNavigating(
        object? sender,
        ShellNavigatingEventArgs e)
    {
        string destination = e.Target.Location.OriginalString;

        if (!destination.Contains(
                "StudentTabs",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var deferral = e.GetDeferral();

        try
        {
            ISessionService sessionService =
                Handler?.MauiContext?.Services
                    .GetService<ISessionService>()
                ?? throw new InvalidOperationException(
                    "Session service is unavailable.");

            string? token = await sessionService.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                e.Cancel();
            }
        }
        catch (Exception)
        {
            e.Cancel();
        }
        finally
        {
            deferral.Complete();
        }
    }
}
