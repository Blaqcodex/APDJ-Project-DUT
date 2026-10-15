using CampusPulse.Student.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampusPulse.Student;

public partial class StudentDashboardPage : ContentPage
{
    private bool isLoadingToday;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTodayClassesAsync();
    }

    private async void OnTodayRetryClicked(
        object? sender,
        EventArgs e)
    {
        await LoadTodayClassesAsync();
    }

    private async Task LoadTodayClassesAsync()
    {
        if (isLoadingToday)
        {
            return;
        }

        isLoadingToday = true;
        TodayLoadingIndicator.IsVisible = true;
        TodayLoadingIndicator.IsRunning = true;
        TodayRetryButton.IsVisible = false;
        TodayStatusLabel.Text = "Loading today's classes...";
        TodayStatusLabel.IsVisible = true;
        TodayClassesContainer.Children.Clear();

        try
        {
            var service = Handler?.MauiContext?.Services
                .GetService<IStudentTimetableApiService>();

            if (service == null)
            {
                throw new InvalidOperationException(
                    "Timetable service is unavailable.");
            }

            var timetable = await service.GetTimetableAsync();

            var todayClasses = timetable
                .Where(entry =>
                    entry.DayOfWeek == DateTime.Today.DayOfWeek)
                .OrderBy(entry => entry.StartTime)
                .ToList();

            if (todayClasses.Count == 0)
            {
                TodayStatusLabel.Text =
                    "No classes scheduled for today.";
                return;
            }

            TodayStatusLabel.IsVisible = false;

            foreach (var entry in todayClasses)
            {
                TodayClassesContainer.Children.Add(
                    CreateTodayClassCard(entry));
            }
        }
        catch (UnauthorizedAccessException)
        {
            ShowTodayError(
                "Your session has expired. Please sign in again.");
        }
        catch (HttpRequestException)
        {
            ShowTodayError(
                "Unable to load today's classes. Check your connection.");
        }
        catch (TaskCanceledException)
        {
            ShowTodayError(
                "The request timed out. Please try again.");
        }
        catch (Exception)
        {
            ShowTodayError(
                "Something went wrong while loading today's classes.");
        }
        finally
        {
            TodayLoadingIndicator.IsRunning = false;
            TodayLoadingIndicator.IsVisible = false;
            isLoadingToday = false;
        }
    }

    private void ShowTodayError(string message)
    {
        TodayStatusLabel.Text = message;
        TodayStatusLabel.IsVisible = true;
        TodayRetryButton.IsVisible = true;
    }

    private static Label CreateThemedLabel(
        string text,
        double fontSize,
        string darkColor,
        string lightColor,
        bool bold = false)
    {
        var label = new Label
        {
            Text = text,
            FontSize = fontSize,
            FontAttributes = bold
                ? FontAttributes.Bold
                : FontAttributes.None
        };

        label.SetAppThemeColor(
            Label.TextColorProperty,
            Color.FromArgb(lightColor),
            Color.FromArgb(darkColor));

        return label;
    }

    private static Border CreateTodayClassCard(
        StudentTimetableEntry entry)
    {
        var details = new VerticalStackLayout
        {
            Spacing = 7
        };

        details.Children.Add(CreateThemedLabel(
            entry.ModuleCode, 13,
            "#FFAD83", "#B95632", true));

        details.Children.Add(CreateThemedLabel(
            entry.ModuleName, 17,
            "#FFFFFF", "#202038", true));

        details.Children.Add(CreateThemedLabel(
            $"{entry.StartTime:hh\\:mm} - {entry.EndTime:hh\\:mm}",
            14, "#FFFFFF", "#202038"));

        details.Children.Add(CreateThemedLabel(
            $"Venue: {entry.VenueName}",
            13, "#B5B4D0", "#686680"));

        details.Children.Add(CreateThemedLabel(
            $"Campus: {entry.Campus}",
            13, "#B5B4D0", "#686680"));

        details.Children.Add(CreateThemedLabel(
            $"Lecturer: {entry.LecturerName}",
            13, "#B5B4D0", "#686680"));

        return new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(4, 8),
            Content = details
        };
    }

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
