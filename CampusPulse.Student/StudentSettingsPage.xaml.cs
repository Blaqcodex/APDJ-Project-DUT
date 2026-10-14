namespace CampusPulse.Student;

public partial class StudentSettingsPage : ContentPage
{
    private bool _isLoading = true;

    public StudentSettingsPage()
    {
        InitializeComponent();

        NotificationsSwitch.IsToggled = Preferences.Default.Get(
            "CampusPulse.NotificationsEnabled",
            true);

        string appearance = Preferences.Default.Get(
            "CampusPulse.Appearance",
            "Dark");

        AppearancePicker.SelectedIndex = appearance switch
        {
            "Light" => 1,
            "System" => 2,
            _ => 0
        };

        _isLoading = false;
    }

    private void OnAppearanceChanged(
        object? sender,
        EventArgs e)
    {
        if (_isLoading)
            return;

        string appearance = AppearancePicker.SelectedIndex switch
        {
            1 => "Light",
            2 => "System",
            _ => "Dark"
        };

        Preferences.Default.Set(
            "CampusPulse.Appearance",
            appearance);

        App.ApplySavedTheme();
    }
    private void OnNotificationPreferenceChanged(
        object? sender,
        ToggledEventArgs e)
    {
        if (_isLoading)
            return;

        Preferences.Default.Set(
            "CampusPulse.NotificationsEnabled",
            e.Value);
    }
}
