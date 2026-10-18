using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace CampusPulse.Admin
{
    public partial class LecturerChangePasswordWindow : Window
    {
        private readonly LecturerApiClient _api = new();

        public LecturerChangePasswordWindow()
        {
            InitializeComponent();
        }

        private async void ChangePassword_Click(
            object sender,
            RoutedEventArgs e)
        {
            string currentPassword = CurrentPasswordInput.Password;
            string newPassword = NewPasswordInput.Password;
            string confirmPassword = ConfirmPasswordInput.Password;

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                StatusText.Text = "Complete all password fields.";
                return;
            }

            if (newPassword.Length < 8)
            {
                StatusText.Text =
                    "New password must contain at least 8 characters.";
                return;
            }

            if (newPassword != confirmPassword)
            {
                StatusText.Text = "New passwords do not match.";
                return;
            }

            if (currentPassword == newPassword)
            {
                StatusText.Text =
                    "New password must differ from the current password.";
                return;
            }

            if (string.IsNullOrWhiteSpace(LecturerSession.Token))
            {
                StatusText.Text =
                    "Your lecturer session is missing. Please sign in again.";
                return;
            }

            ChangePasswordButton.IsEnabled = false;
            ChangePasswordButton.Content = "Changing password...";
            StatusText.Text = "";

            try
            {
                await _api.ChangePasswordAsync(
                    LecturerSession.Token,
                    currentPassword,
                    newPassword);

                CurrentPasswordInput.Clear();
                NewPasswordInput.Clear();
                ConfirmPasswordInput.Clear();

                MessageBox.Show(
                    "Your password has been changed successfully.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var dashboard = new MainWindow();

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
                        : "Password change failed. Please try again.";
            }
            finally
            {
                ChangePasswordButton.IsEnabled = true;
                ChangePasswordButton.Content = "Change Password";
            }
        }
    }
}