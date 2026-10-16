using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace CampusPulse.Admin
{
    public partial class LecturerAnnouncementsWindow : Window
    {
        private readonly HttpClient _http = new()
        {
            BaseAddress = new Uri("http://localhost:5240/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        private bool _publishing;

        public LecturerAnnouncementsWindow()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                await LoadModulesAsync();
                await LoadHistoryAsync();
            };
        }

        private HttpRequestMessage CreateRequest(
            HttpMethod method,
            string path)
        {
            var request = new HttpRequestMessage(method, path);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    LecturerSession.Token);

            return request;
        }

        private async Task LoadModulesAsync()
        {
            if (!LecturerSession.IsAuthenticated)
            {
                PublishStatusText.Text = "Please sign in again.";
                return;
            }

            try
            {
                using var request = CreateRequest(
                    HttpMethod.Get,
                    "api/lecturer/announcements/modules");

                using var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    PublishStatusText.Text =
                        $"Unable to load modules (HTTP {(int)response.StatusCode}).";
                    return;
                }

                var modules = await response.Content
                    .ReadFromJsonAsync<List<AnnouncementModule>>();

                ModuleComboBox.ItemsSource = modules;

                if (modules?.Count > 0)
                    ModuleComboBox.SelectedIndex = 0;
                else
                    PublishStatusText.Text =
                        "No assigned modules found for your account.";
            }
            catch (Exception)
            {
                PublishStatusText.Text =
                    "Unable to connect to the CampusPulse API.";
            }
        }

        private async void Publish_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_publishing)
                return;

            if (!LecturerSession.IsAuthenticated)
            {
                PublishStatusText.Text = "Please sign in again.";
                return;
            }

            if (ModuleComboBox.SelectedItem is not AnnouncementModule module)
            {
                PublishStatusText.Text = "Please select a module.";
                return;
            }

            string title = TitleTextBox.Text.Trim();
            string message = MessageTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(message))
            {
                PublishStatusText.Text =
                    "Enter both a title and a message.";
                return;
            }

            var confirmation = MessageBox.Show(
                $"Publish this announcement to students enrolled in {module.ModuleCode}?",
                "Confirm Announcement",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;

            _publishing = true;
            PublishButton.IsEnabled = false;
            PublishStatusText.Text = "Publishing announcement...";

            try
            {
                using var request = CreateRequest(
                    HttpMethod.Post,
                    "api/lecturer/announcements");

                request.Content = JsonContent.Create(new
                {
                    moduleId = module.ModuleId,
                    title,
                    message
                });

                using var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    PublishStatusText.Text =
                        $"Publishing failed (HTTP {(int)response.StatusCode}).";
                    return;
                }

                var result = await response.Content
                    .ReadFromJsonAsync<PublishAnnouncementResult>();

                PublishStatusText.Text =
                    $"Published successfully. {result?.RecipientCount ?? 0} student notification(s) created.";

                TitleTextBox.Clear();
                MessageTextBox.Clear();

                await LoadHistoryAsync();
            }
            catch (TaskCanceledException)
            {
                PublishStatusText.Text = "The publishing request timed out.";
            }
            catch (Exception)
            {
                PublishStatusText.Text =
                    "Unable to publish. Check the API connection before retrying.";
            }
            finally
            {
                _publishing = false;
                PublishButton.IsEnabled = true;
            }
        }

        private async void Refresh_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            if (!LecturerSession.IsAuthenticated)
                return;

            HistoryStatusText.Text = "Loading announcements...";

            try
            {
                using var request = CreateRequest(
                    HttpMethod.Get,
                    "api/lecturer/announcements");

                using var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    HistoryStatusText.Text =
                        $"Unable to load history (HTTP {(int)response.StatusCode}).";
                    return;
                }

                var announcements = await response.Content
                    .ReadFromJsonAsync<List<PublishedAnnouncement>>();

                AnnouncementsGrid.ItemsSource = announcements;

                HistoryStatusText.Text =
                    $"{announcements?.Count ?? 0} published announcement(s).";
            }
            catch (Exception)
            {
                HistoryStatusText.Text =
                    "Unable to load announcement history.";
            }
        }

        private sealed class AnnouncementModule
        {
            [JsonPropertyName("moduleId")]
            public int ModuleId { get; set; }

            [JsonPropertyName("moduleCode")]
            public string ModuleCode { get; set; } = "";

            [JsonPropertyName("moduleName")]
            public string ModuleName { get; set; } = "";

            public string DisplayName =>
                $"{ModuleCode} - {ModuleName}";
        }

        private sealed class PublishAnnouncementResult
        {
            [JsonPropertyName("recipientCount")]
            public int RecipientCount { get; set; }
        }

        private sealed class PublishedAnnouncement
        {
            [JsonPropertyName("moduleId")]
            public int? ModuleId { get; set; }

            [JsonPropertyName("moduleCode")]
            public string ModuleCode { get; set; } = "";

            [JsonPropertyName("moduleName")]
            public string ModuleName { get; set; } = "";

            [JsonPropertyName("title")]
            public string Title { get; set; } = "";

            [JsonPropertyName("message")]
            public string Message { get; set; } = "";

            [JsonPropertyName("createdAt")]
            public DateTime CreatedAt { get; set; }
        }
    }
}