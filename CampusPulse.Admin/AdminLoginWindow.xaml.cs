using System;
using System.Net.Http;
using System.Windows;

namespace CampusPulse.Admin
{
    public partial class AdminLoginWindow : Window
    {
        private readonly AdminApiClient _api = new();

        public AdminLoginWindow()
        {
            InitializeComponent();
        }

        private async void SignIn_Click(
            object sender,
            RoutedEventArgs e)
        {
            string email = EmailInput.Text.Trim();
            string password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                StatusText.Text = "Enter your email and password.";
                return;
            }

            SignInButton.IsEnabled = false;
            SignInButton.Content = "Signing in...";
            StatusText.Text = "";

            try
            {
                var result = await _api.LoginAsync(email, password);

                AdminSession.SignIn(result.Token, result.DisplayName);
                PasswordInput.Clear();

                var dashboard = new AdminDashboardWindow();
                System.Windows.Application.Current.MainWindow = dashboard;
                dashboard.Show();

                Close();
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
            catch (Exception ex)
            {
                StatusText.Text =
                    ex is InvalidOperationException
                        ? ex.Message
                        : "Sign-in failed. Please try again.";
            }
            finally
            {
                SignInButton.IsEnabled = true;
                SignInButton.Content = "Sign In";
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            PasswordInput.Clear();

            var selection = new PortalSelectionWindow();
            System.Windows.Application.Current.MainWindow = selection;
            selection.Show();

            Close();
        }
    }
}