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
    public partial class AdminUsersWindow : Window
    {
        private readonly AdminApiClient _api = new();
        private List<AdminUserRecord> _users = new();
        private bool _loaded;

        public AdminUsersWindow()
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
                await LoadUsersAsync();
            };
        }

        private async Task LoadUsersAsync()
        {
            RefreshButton.IsEnabled = false;
            StatusText.Text = "Loading registered users...";

            try
            {
                string? role = GetSelectedRole();

                _users = await _api.GetUsersAsync(role);

                ApplySearch();
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.Unauthorized ||
                      ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _users.Clear();
                UsersGrid.ItemsSource = null;

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
                    "Unable to load users. Please try again.";
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private string? GetSelectedRole()
        {
            if (RoleFilter.SelectedItem is ComboBoxItem item)
            {
                string? role = item.Content?.ToString();

                if (role == "Student" ||
                    role == "Lecturer" ||
                    role == "Admin")
                {
                    return role;
                }
            }

            return null;
        }

        private void ApplySearch()
        {
            string search = SearchInput.Text.Trim();

            var filtered = _users.Where(user =>
                string.IsNullOrWhiteSpace(search) ||
                user.FirstName.Contains(
                    search, StringComparison.OrdinalIgnoreCase) ||
                user.LastName.Contains(
                    search, StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains(
                    search, StringComparison.OrdinalIgnoreCase) ||
                user.Role.Contains(
                    search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            UsersGrid.ItemsSource = filtered;

            StatusText.Text =
                $"{filtered.Count} user(s) displayed.";
        }

        private async void RoleFilter_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_loaded)
            {
                return;
            }

            await LoadUsersAsync();
        }

        private void SearchInput_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (!_loaded)
            {
                return;
            }

            ApplySearch();
        }

        private async void Refresh_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private void Back_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dashboard = new AdminDashboardWindow();

            System.Windows.Application.Current.MainWindow = dashboard;
            dashboard.Show();

            Close();
        }
    }
}