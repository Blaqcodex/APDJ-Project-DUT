using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CampusPulse.Admin
{
    public partial class AdminProgrammesWindow : Window
    {
        private readonly AdminApiClient _api = new();
        private List<AdminProgrammeRecord> _programmes = new();
        private bool _loaded;

        public AdminProgrammesWindow()
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            InitializeComponent();

            Loaded += async (_, _) =>
            {
                if (_loaded)
                {
                    return;
                }

                _loaded = true;
                await LoadProgrammesAsync();
            };
        }

        private async Task LoadProgrammesAsync()
        {
            RefreshButton.IsEnabled = false;
            StatusText.Text = "Loading programmes...";

            try
            {
                _programmes = await _api.GetProgrammesAsync();
                ApplySearch();
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.Unauthorized ||
                      ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _programmes.Clear();
                ProgrammesGrid.ItemsSource = null;
                StatusText.Text =
                    "Your administrator session is no longer authorized. Please sign in again.";
            }
            catch (HttpRequestException)
            {
                StatusText.Text =
                    "Cannot connect to CampusPulse API. Check that it is running.";
            }
            catch (TaskCanceledException)
            {
                StatusText.Text =
                    "The request timed out. Please try again.";
            }
            catch (Exception)
            {
                StatusText.Text =
                    "Unable to load programmes. Please try again.";
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private void ApplySearch()
        {
            string search = SearchInput.Text.Trim();

            var filtered = _programmes.Where(programme =>
                string.IsNullOrWhiteSpace(search) ||
                programme.ProgrammeName.Contains(
                    search, StringComparison.OrdinalIgnoreCase) ||
                programme.Faculty.Contains(
                    search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ProgrammesGrid.ItemsSource = filtered;
            StatusText.Text = $"{filtered.Count} programme(s) displayed.";
        }

        private void SearchInput_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_loaded)
            {
                ApplySearch();
            }
        }

        private async void Refresh_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadProgrammesAsync();
        }

        private async void AddProgramme_Click(
            object sender,
            RoutedEventArgs e)
        {
            string name = NameInput.Text.Trim();
            string faculty = FacultyInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(faculty))
            {
                FormStatusText.Text =
                    "Programme name and faculty are required.";
                return;
            }

            var confirmation = MessageBox.Show(
                $"Create programme '{name}' in faculty '{faculty}'?",
                "Confirm New Programme",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            AddButton.IsEnabled = false;
            FormStatusText.Text = "Creating programme...";

            try
            {
                await _api.CreateProgrammeAsync(name, faculty);

                NameInput.Clear();
                FacultyInput.Clear();

                FormStatusText.Text = "Programme created successfully.";
                await LoadProgrammesAsync();
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                FormStatusText.Text =
                    "This programme already exists in the selected faculty.";
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                FormStatusText.Text =
                    "Invalid programme details. Check your inputs.";
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.Unauthorized ||
                      ex.StatusCode == HttpStatusCode.Forbidden)
            {
                FormStatusText.Text =
                    "Administrator authorization failed. Please sign in again.";
            }
            catch (HttpRequestException)
            {
                FormStatusText.Text =
                    "Cannot connect to CampusPulse API.";
            }
            catch (TaskCanceledException)
            {
                FormStatusText.Text =
                    "The request timed out. Please check the programme list before retrying.";
            }
            catch (Exception)
            {
                FormStatusText.Text =
                    "Unable to create programme. Please try again.";
            }
            finally
            {
                AddButton.IsEnabled = true;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new AdminDashboardWindow();

            System.Windows.Application.Current.MainWindow = dashboard;
            dashboard.Show();

            Close();
        }
    }
}