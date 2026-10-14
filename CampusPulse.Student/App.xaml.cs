namespace CampusPulse.Student;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
        ApplySavedTheme();
    }

    public static void ApplySavedTheme()
    {
        if (Current is null)
            return;

        string preference = Preferences.Default.Get(
            "CampusPulse.Appearance",
            "Dark");

        Current.UserAppTheme = preference switch
        {
            "Light" => AppTheme.Light,
            "System" => AppTheme.Unspecified,
            _ => AppTheme.Dark
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
