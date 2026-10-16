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
    public partial class AdminModulesWindow : Window
    {
        private readonly AdminApiClient _api = new();
        private List<AdminModuleRecord> _modules = new();
        private bool _loaded;

        public AdminModulesWindow()
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
                await LoadModulesAsync();
            };
        }

        private async Task LoadModulesAsync()
        {
            RefreshButton.IsEnabled = false;
            StatusText.Text = "Loading modules...";

            try
            {
                _modules = await _api.GetModulesAsync();
                ApplySearch();
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.Unauthorized ||
                      ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _modules.Clear();
                ModulesGrid.ItemsSource = null;
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
                    "Unable to load modules. Please try again.";
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private void ApplySearch()
        {
            string search = SearchInput.Text.Trim();

            var filtered = _modules.Where(module =>
                string.IsNullOrWhiteSpace(search) ||
                module.ModuleCode.Contains(
                    search, StringComparison.OrdinalIgnoreCase) ||
                module.ModuleName.Contains(
                    search, StringComparison.OrdinalIgnoreCase) ||
                module.ProgrammeName.Contains(
                    search, StringComparison.OrdinalIgnoreCase) ||
                module.Faculty.Contains(
                    search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ModulesGrid.ItemsSource = filtered;
            StatusText.Text = $"{filtered.Count} module(s) displayed.";
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
            await LoadModulesAsync();
        }

        private async void AddModule_Click(
            object sender,
            RoutedEventArgs e)
        {
            string code = CodeInput.Text.Trim().ToUpperInvariant();
            string name = NameInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(name))
            {
                FormStatusText.Text =
                    "Module code and module name are required.";
                return;
            }

            if (!int.TryParse(ProgrammeInput.Text.Trim(), out int programmeId) ||
                programmeId <= 0)
            {
                FormStatusText.Text =
                    "Enter a valid positive Programme ID.";
                return;
            }

            var confirmation = MessageBox.Show(
                $"Create module {code} ({name}) under Programme ID {programmeId}?",
                "Confirm New Module",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            AddButton.IsEnabled = false;
            FormStatusText.Text = "Creating module...";

            try
            {
                await _api.CreateModuleAsync(code, name, programmeId);

                CodeInput.Clear();
                NameInput.Clear();
                ProgrammeInput.Clear();

                FormStatusText.Text = "Module created successfully.";
                await LoadModulesAsync();
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                FormStatusText.Text =
                    "This module code already exists in the selected programme.";
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                FormStatusText.Text =
                    "Invalid module details or programme. Check your inputs.";
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
                    "The request timed out. Please check the module list before retrying.";
            }
            catch (Exception)
            {
                FormStatusText.Text =
                    "Unable to create module. Please try again.";
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