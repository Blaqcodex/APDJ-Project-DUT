using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;

namespace CampusPulse.Admin
{
    public partial class AdminLecturerProvisioningWindow : Window
    {
        private readonly AdminApiClient _api = new();
        private bool _busy;

        public AdminLecturerProvisioningWindow()
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            InitializeComponent();
        }

        private async void CreateLecturer_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_busy)
            {
                return;
            }

            string firstName = FirstNameInput.Text.Trim();
            string lastName = LastNameInput.Text.Trim();
            string email = EmailInput.Text.Trim().ToLowerInvariant();
            string staffNumber = StaffNumberInput.Text.Trim();
            string department = DepartmentInput.Text.Trim();

            string temporaryPassword = TemporaryPasswordInput.Password;
            string confirmation = ConfirmPasswordInput.Password;

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(staffNumber) ||
                string.IsNullOrWhiteSpace(department))
            {
                ShowStatus("Complete all lecturer details.");
                return;
            }

            try
            {
                var parsedEmail = new MailAddress(email);

                if (!string.Equals(
                    parsedEmail.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase))
                {
                    ShowStatus("Enter a valid email address.");
                    return;
                }
            }
            catch (FormatException)
            {
                ShowStatus("Enter a valid email address.");
                return;
            }

            if (temporaryPassword.Length < 12 ||
                temporaryPassword.Length > 128)
            {
                ShowStatus(
                    "Temporary password must contain 12-128 characters.");
                return;
            }

            if (temporaryPassword != confirmation)
            {
                ShowStatus("Temporary passwords do not match.");
                return;
            }

            var confirmationResult = MessageBox.Show(
                $"Create a lecturer account for {firstName} {lastName}?\n\n" +
                $"Email: {email}\nStaff number: {staffNumber}\n" +
                $"Department: {department}\n\n" +
                "The lecturer will be required to change the temporary password.",
                "Confirm Lecturer Creation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmationResult != MessageBoxResult.Yes)
            {
                return;
            }

            SetBusy(true);
            ShowStatus("Creating lecturer account...");

            try
            {
                var request = new AdminCreateLecturerRequest(
                    firstName,
                    lastName,
                    email,
                    staffNumber,
                    department,
                    temporaryPassword);

                var result = await _api.CreateLecturerAsync(request);

                TemporaryPasswordInput.Clear();
                ConfirmPasswordInput.Clear();

                ShowStatus(
                    $"Lecturer account created successfully. " +
                    $"Staff number: {result.StaffNumber}. " +
                    "A password change is required at first login.");

                MessageBox.Show(
                    "Lecturer account created successfully.",
                    "Lecturer Provisioning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (AdminApiException ex)
            {
                string message = ex.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or
                    HttpStatusCode.Forbidden =>
                        "Administrator session is no longer authorized.",

                    HttpStatusCode.BadRequest =>
                        "The lecturer details or temporary password are invalid.",

                    HttpStatusCode.Conflict =>
                        "The email address or staff number already exists.",

                    _ => "Unable to create the lecturer account."
                };

                ShowStatus(message);
            }
            catch (HttpRequestException)
            {
                ShowStatus("Cannot connect to the CampusPulse API.");
            }
            catch (TaskCanceledException)
            {
                ShowStatus("The API request timed out.");
            }
            catch (Exception)
            {
                ShowStatus("An unexpected error occurred.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ShowStatus(string message)
        {
            StatusText.Text = message;
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            CreateLecturerButton.IsEnabled = !busy;
        }

        private void Back_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_busy)
            {
                return;
            }

            var dashboard = new AdminDashboardWindow();

            System.Windows.Application.Current.MainWindow = dashboard;
            dashboard.Show();

            Close();
        }
    }
}