using CampusPulse.Student.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampusPulse.Student;

public partial class StudentTimetablePage : ContentPage
{
    private bool isLoading;

    public StudentTimetablePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTimetableAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadTimetableAsync();
    }

    private async Task LoadTimetableAsync()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        RetryButton.IsVisible = false;
        StatusLabel.IsVisible = false;
        TimetableContainer.Children.Clear();

        try
        {
            var service = Handler?.MauiContext?.Services
                .GetService<IStudentTimetableApiService>();

            if (service == null)
            {
                throw new InvalidOperationException(
                    "The timetable service is unavailable.");
            }

            var entries = await service.GetTimetableAsync();

            if (entries.Count == 0)
            {
                StatusLabel.Text =
                    "No classes are scheduled for your registered modules.";
                StatusLabel.IsVisible = true;
                return;
            }

            var groupedEntries = entries
                .OrderBy(entry => entry.DayOfWeek)
                .ThenBy(entry => entry.StartTime)
                .GroupBy(entry => entry.DayOfWeek);

            foreach (var group in groupedEntries)
            {
                var daySection = new VerticalStackLayout
                {
                    Spacing = 12
                };

                daySection.Children.Add(new Label
                {
                    Text = group.Key.ToString().ToUpperInvariant(),
                    FontSize = 13,
                    CharacterSpacing = 2,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FFAD83")
                });

                foreach (var entry in group)
                {
                    daySection.Children.Add(CreateClassCard(entry));
                }

                TimetableContainer.Children.Add(daySection);
            }
        }
        catch (UnauthorizedAccessException)
        {
            ShowError("Your session has expired. Please sign in again.");
        }
        catch (HttpRequestException)
        {
            ShowError(
                "Unable to load your timetable. Check your connection and try again.");
        }
        catch (TaskCanceledException)
        {
            ShowError("The request timed out. Please try again.");
        }
        catch (Exception)
        {
            ShowError(
                "Something went wrong while loading your timetable.");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            isLoading = false;
        }
    }

    private void ShowError(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.IsVisible = true;
        RetryButton.IsVisible = true;
    }

    private static Border CreateClassCard(StudentTimetableEntry entry)
    {
        var details = new VerticalStackLayout
        {
            Spacing = 9
        };

        details.Children.Add(new Label
        {
            Text = entry.ModuleCode,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFAD83")
        });

        details.Children.Add(new Label
        {
            Text = entry.ModuleName,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFFFFF")
        });

        details.Children.Add(new Label
        {
            Text =
                $"{entry.StartTime:hh\\:mm} - {entry.EndTime:hh\\:mm}",
            FontSize = 15,
            TextColor = Color.FromArgb("#FFFFFF")
        });

        details.Children.Add(new Label
        {
            Text = $"Venue: {entry.VenueName}",
            FontSize = 13,
            TextColor = Color.FromArgb("#B5B4D0")
        });

        details.Children.Add(new Label
        {
            Text = $"Campus: {entry.Campus}",
            FontSize = 13,
            TextColor = Color.FromArgb("#B5B4D0")
        });

        details.Children.Add(new Label
        {
            Text = $"Lecturer: {entry.LecturerName}",
            FontSize = 13,
            TextColor = Color.FromArgb("#B5B4D0")
        });

        return new Border
        {
            BackgroundColor = Color.FromArgb("#242342"),
            Stroke = Color.FromArgb("#38365C"),
            StrokeThickness = 1,
            Padding = new Thickness(20),
            StrokeShape =
                new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(18)
                },
            Content = details
        };
    }
}