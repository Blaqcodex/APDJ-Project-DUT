using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace CampusPulse.Admin
{
    public partial class CreateLecturerWindow : Window
    {
        private readonly AdminApiClient _api = new();
        private bool _saving;

        public CreateLecturerWindow()
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Administrator authentication is required.");
            }

            InitializeComponent();
        }

        private async void Create_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_saving)
                return;

            string firstName = FirstNameInput.Text.Trim();
            string lastName = LastNameInput.Text.Trim();
            string email = EmailInput.Text.Trim();
            string staffNumber = StaffNumberInput.Text.Trim();
            string department = DepartmentInput.Text.Trim();
            string password = PasswordInput.Password;
            string confirmation = ConfirmPasswordInput.Password;

            if (firstName.Length is < 1 or > 100 ||
                lastName.Length is < 1 or > 100 ||
                staffNumber.Length is < 1 or > 50 ||
                department.Length is < 1 or > 150)
            {
                FormStatusText.Text =
                    "Check the required fields and their lengths.";
                return;
            }

            if (email.Length is < 3 or > 256 ||
                !new EmailAddressAttribute().IsValid(email))
            {
                FormStatusText.Text =
                    "Enter a valid email address.";
                return;
            }

            if (password.Length < 8 || password.Length > 128)
            {
                FormStatusText.Text =
                    "Password must contain 8 to 128 characters.";
                return;
            }

            if (password != confirmation)
            {
                FormStatusText.Text =
                    "Passwords do not match.";
                return;
            }

            var confirmationResult = MessageBox.Show(
                $"Create a Lecturer account for {firstName} {lastName} " +
                $"({email})?",
                "Confirm Lecturer Creation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmationResult != MessageBoxResult.Yes)
                return;

            _saving = true;
            CreateButton.IsEnabled = false;
            FormStatusText.Text = "Creating lecturer account...";

            try
            {
                await _api.CreateLecturerAsync(
                    firstName,
                    lastName,
                    email,
                    staffNumber,
                    department,
                    password);

                MessageBox.Show(
                    "Lecturer account created successfully.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                FormStatusText.Text =
                    "This email or staff number is already registered.";
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                FormStatusText.Text =
                    "The API rejected the details. Review all fields.";
            }
            catch (AdminApiException ex)
                when (ex.StatusCode == HttpStatusCode.Unauthorized ||
                      ex.StatusCode == HttpStatusCode.Forbidden)
            {
                FormStatusText.Text =
                    "Administrator authorization failed. Sign in again.";
            }
            catch (HttpRequestException)
            {
                FormStatusText.Text =
                    "Cannot connect to CampusPulse API.";
            }
            catch (TaskCanceledException)
            {
                FormStatusText.Text =
                    "The request timed out. Please try again.";
            }
            catch (Exception)
            {
                FormStatusText.Text =
                    "Unable to create the lecturer account.";
            }
            finally
            {
                _saving = false;
                CreateButton.IsEnabled = true;
            }
        }

        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
