using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CampusPulse.Admin
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _http = new()
        {
            BaseAddress = new Uri("http://localhost:5240/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        private bool _loadingClasses;

        public MainWindow()
        {
            InitializeComponent();
            ShowOverview();
        }

        private void ShowOverview()
        {
            PageHeading.Text = "Overview";
            OverviewPanel.Visibility = Visibility.Visible;
            ClassesPanel.Visibility = Visibility.Collapsed;
            AttendancePanel.Visibility = Visibility.Collapsed;
        }

        private void Overview_Click(object sender, RoutedEventArgs e)
        {
            ShowOverview();
        }

        private async void Classes_Click(object sender, RoutedEventArgs e)
        {
            PageHeading.Text = "My Classes";
            OverviewPanel.Visibility = Visibility.Collapsed;
            ClassesPanel.Visibility = Visibility.Visible;
            AttendancePanel.Visibility = Visibility.Collapsed;

            await LoadClassesAsync();
        }

        private async void RefreshClasses_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadClassesAsync();
        }

        private async Task LoadClassesAsync()
        {
            if (_loadingClasses)
                return;

            if (!LecturerSession.IsAuthenticated)
            {
                ClassesStatusText.Text = "Please sign in to view your classes.";
                ClassesGrid.ItemsSource = null;
                return;
            }

            _loadingClasses = true;
            ClassesStatusText.Text = "Loading your assigned classes...";

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    "api/lecturer/timetable");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        LecturerSession.Token);

                using var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    ClassesGrid.ItemsSource = null;
                    ClassesStatusText.Text =
                        $"Unable to load classes (HTTP {(int)response.StatusCode}).";
                    return;
                }

                var classes = await response.Content
                    .ReadFromJsonAsync<List<LecturerClass>>();

                classes ??= new List<LecturerClass>();

                ClassesGrid.ItemsSource = classes;

                ClassesStatusText.Text = classes.Count == 0
                    ? "No classes are currently assigned to your lecturer account."
                    : $"{classes.Count} assigned class session(s).";
            }
            catch (HttpRequestException)
            {
                ClassesGrid.ItemsSource = null;
                ClassesStatusText.Text =
                    "Cannot connect to CampusPulse API. Check that the API is running.";
            }
            catch (TaskCanceledException)
            {
                ClassesGrid.ItemsSource = null;
                ClassesStatusText.Text = "The timetable request timed out.";
            }
            catch (Exception)
            {
                ClassesGrid.ItemsSource = null;
                ClassesStatusText.Text =
                    "An unexpected error occurred while loading classes.";
            }
            finally
            {
                _loadingClasses = false;
            }
        }

        private void StartClassAttendance_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!LecturerSession.IsAuthenticated)
                return;

            if (sender is not Button button ||
                button.Tag is not int timetableId)
                return;

            var window = new LecturerAttendanceWindow(timetableId)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void ViewClassAnalytics_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!LecturerSession.IsAuthenticated)
            {
                MessageBox.Show(
                    "Please sign in to view attendance analytics.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (sender is not Button button ||
                button.Tag is not int moduleId ||
                moduleId <= 0)
            {
                MessageBox.Show(
                    "This class does not have a valid module ID.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var window = new LecturerAttendanceAnalyticsWindow(moduleId)
            {
                Owner = this
            };

            window.ShowDialog();
        }
        private void Attendance_Click(object sender, RoutedEventArgs e)
        {
            PageHeading.Text = "Attendance";
            OverviewPanel.Visibility = Visibility.Collapsed;
            ClassesPanel.Visibility = Visibility.Collapsed;
            AttendancePanel.Visibility = Visibility.Visible;
        }

        private void Announcements_Click(object sender, RoutedEventArgs e)
        {
            if (!LecturerSession.IsAuthenticated)
            {
                MessageBox.Show(
                    "Please sign in before managing announcements.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var window = new LecturerAnnouncementsWindow
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void StartAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (!LecturerSession.IsAuthenticated)
            {
                MessageBox.Show(
                    "Please sign in before managing attendance.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var window = new LecturerAttendanceWindow
            {
                Owner = this
            };

            window.ShowDialog();
        }
    }

    public sealed class LecturerClass
    {
        [JsonPropertyName("timetableId")]
        public int TimetableId { get; set; }

        [JsonPropertyName("moduleId")]
        public int ModuleId { get; set; }

        [JsonPropertyName("moduleCode")]
        public string ModuleCode { get; set; } = "";

        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; } = "";

        [JsonPropertyName("dayOfWeek")]
        public string DayOfWeek { get; set; } = "";

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = "";

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; } = "";

        [JsonPropertyName("venueName")]
        public string VenueName { get; set; } = "";

        [JsonPropertyName("campus")]
        public string Campus { get; set; } = "";

        public string TimeDisplay => $"{StartTime} - {EndTime}";
    }}