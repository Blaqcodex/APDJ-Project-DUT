using CampusPulse.Student.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampusPulse.Student;

public partial class StudentNotificationsPage : ContentPage
{
    private bool isLoading;
    private bool isMarkingRead;

    public StudentNotificationsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNotificationsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadNotificationsAsync();
    }

    private IStudentNotificationsApiService GetNotificationsService()
    {
        return Handler?.MauiContext?.Services
            .GetService<IStudentNotificationsApiService>()
            ?? throw new InvalidOperationException(
                "The notifications service is unavailable.");
    }

    private async Task LoadNotificationsAsync()
    {
        if (isLoading || isMarkingRead)
        {
            return;
        }

        isLoading = true;
        RefreshButton.IsEnabled = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        StatusCard.IsVisible = false;
        NotificationsContainer.Children.Clear();
        UnreadCountLabel.Text = "Loading notifications...";

        try
        {
            var service = GetNotificationsService();

            var notifications = await service.GetNotificationsAsync();
            int unreadCount = await service.GetUnreadCountAsync();

            UnreadCountLabel.Text = unreadCount == 1
                ? "1 unread notification"
                : $"{unreadCount} unread notifications";

            if (notifications.Count == 0)
            {
                ShowStatus(
                    "🔔",
                    "You're all caught up",
                    "Your university announcements and important updates will appear here.");
                return;
            }

            foreach (var notification in notifications
                .OrderByDescending(n => n.CreatedAt))
            {
                NotificationsContainer.Children.Add(
                    CreateNotificationCard(notification));
            }
        }
        catch (UnauthorizedAccessException)
        {
            ShowError(
                "Your session has expired. Please sign in again.");
        }
        catch (HttpRequestException)
        {
            ShowError(
                "Unable to load notifications. Check your connection and refresh.");
        }
        catch (TaskCanceledException)
        {
            ShowError(
                "The request timed out. Please refresh.");
        }
        catch (Exception)
        {
            ShowError(
                "Something went wrong while loading notifications.");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            RefreshButton.IsEnabled = true;
            isLoading = false;
        }
    }

    private Border CreateNotificationCard(StudentNotification notification)
    {
        var details = new VerticalStackLayout
        {
            Spacing = 10
        };

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };

        var title = new Label
        {
            Text = notification.Title,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetThemeColor("#FFFFFF", "#202038"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        header.Add(title);
        Grid.SetColumn(title, 0);

        var badge = new Label
        {
            Text = notification.IsRead ? "READ" : "NEW",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = notification.IsRead
                ? GetThemeColor("#B5B4D0", "#686680")
                : GetThemeColor("#FFAD83", "#B95632"),
            VerticalOptions = LayoutOptions.Start
        };

        header.Add(badge);
        Grid.SetColumn(badge, 1);

        details.Children.Add(header);

        details.Children.Add(new Label
        {
            Text = notification.Message,
            FontSize = 14,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = GetThemeColor("#B5B4D0", "#686680")
        });

        details.Children.Add(new Label
        {
            Text = notification.CreatedAt.ToLocalTime()
                .ToString("dd MMM yyyy, HH:mm"),
            FontSize = 12,
            TextColor = GetThemeColor("#B5B4D0", "#686680")
        });

        if (!notification.IsRead)
        {
            var markReadButton = new Button
            {
                Text = "Mark as read",
                FontSize = 13,
                BackgroundColor = GetThemeColor("#38365C", "#E4E3F0"),
                TextColor = GetThemeColor("#FFFFFF", "#202038"),
                CornerRadius = 12,
                HorizontalOptions = LayoutOptions.Start
            };

            markReadButton.Clicked += async (_, _) =>
            {
                if (isLoading || isMarkingRead)
                {
                    return;
                }

                isMarkingRead = true;
                markReadButton.IsEnabled = false;
                RefreshButton.IsEnabled = false;

                try
                {
                    await GetNotificationsService()
                        .MarkAsReadAsync(notification.NotificationId);

                    notification.IsRead = true;
                    badge.Text = "READ";
                    badge.TextColor = GetThemeColor("#B5B4D0", "#686680");
                    details.Children.Remove(markReadButton);

                    await RefreshUnreadCountAsync();
                }
                catch (UnauthorizedAccessException)
                {
                    await DisplayAlertAsync(
                        "Session expired",
                        "Please sign in again.",
                        "OK");
                }
                catch (Exception)
                {
                    await DisplayAlertAsync(
                        "Unable to update",
                        "Could not mark this notification as read. Please try again.",
                        "OK");

                    markReadButton.IsEnabled = true;
                }
                finally
                {
                    isMarkingRead = false;
                    RefreshButton.IsEnabled = true;
                }
            };

            details.Children.Add(markReadButton);
        }

        return new Border
        {
            BackgroundColor = notification.IsRead
                ? GetThemeColor("#242342", "#FFFFFF")
                : GetThemeColor("#302D51", "#FFF4EC"),
            Stroke = GetThemeColor("#38365C", "#E4E3F0"),
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

    private async Task RefreshUnreadCountAsync()
    {
        try
        {
            int unreadCount = await GetNotificationsService()
                .GetUnreadCountAsync();

            UnreadCountLabel.Text = unreadCount == 1
                ? "1 unread notification"
                : $"{unreadCount} unread notifications";
        }
        catch (Exception)
        {
            UnreadCountLabel.Text =
                "Refresh to update unread count";
        }
    }

    private void ShowError(string message)
    {
        UnreadCountLabel.Text = "Notifications unavailable";
        ShowStatus("⚠️", "Unable to load notifications", message);
    }

    private void ShowStatus(string icon, string title, string message)
    {
        StatusIcon.Text = icon;
        StatusTitle.Text = title;
        StatusMessage.Text = message;
        StatusCard.IsVisible = true;
    }

    private static Color GetThemeColor(
        string darkHex,
        string lightHex)
    {
        return Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb(darkHex)
            : Color.FromArgb(lightHex);
    }
}