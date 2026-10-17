using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace CampusPulse.Admin
{
    public partial class AdminLecturerAssignmentsWindow : Window
    {
        private readonly AdminApiClient _api = new();

        public AdminLecturerAssignmentsWindow()
        {
            InitializeComponent();

            Loaded += async (_, _) => await LoadDataAsync();
        }

        private void BackToDashboard_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dashboard = new AdminDashboardWindow();

            System.Windows.Application.Current.MainWindow = dashboard;
            dashboard.Show();

            Close();
        }
        private async Task LoadDataAsync()
        {
            SetBusy(true);
            StatusText.Text = "Loading lecturer assignments...";

            try
            {
                var lecturersTask = _api.GetLecturersAsync();
                var modulesTask = _api.GetModulesAsync();
                var assignmentsTask = _api.GetLecturerAssignmentsAsync();

                await Task.WhenAll(
                    lecturersTask,
                    modulesTask,
                    assignmentsTask);

                LecturerComboBox.ItemsSource = await lecturersTask;
                ModuleComboBox.ItemsSource = await modulesTask;

                var assignments = await assignmentsTask;

                AssignmentsGrid.ItemsSource = assignments;

                AssignmentCountText.Text =
                    $"{assignments.Count} assignment(s)";

                StatusText.Text = "Assignments loaded successfully.";
            }
            catch (Exception ex)
            {
                ShowError("Unable to load assignments.", ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void Refresh_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void Assign_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (LecturerComboBox.SelectedItem is not AdminLecturerRecord lecturer ||
                ModuleComboBox.SelectedItem is not AdminModuleRecord module)
            {
                MessageBox.Show(
                    "Select both a lecturer and a module.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            SetBusy(true);

            try
            {
                await _api.AssignLecturerAsync(
                    lecturer.LecturerId,
                    module.ModuleId);

                await LoadDataAsync();

                StatusText.Text =
                    $"Assigned {lecturer.FirstName} {lecturer.LastName} to {module.ModuleCode}.";
            }
            catch (Exception ex)
            {
                ShowError("Unable to assign lecturer.", ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void Remove_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (AssignmentsGrid.SelectedItem is not
                AdminLecturerAssignmentRecord assignment)
            {
                MessageBox.Show(
                    "Select an assignment to remove.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var confirmation = MessageBox.Show(
                $"Remove {assignment.LecturerName} from {assignment.ModuleCode}?",
                "Confirm Assignment Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            SetBusy(true);

            try
            {
                await _api.RemoveLecturerAssignmentAsync(
                    assignment.LecturerModuleId);

                await LoadDataAsync();

                StatusText.Text = "Assignment removed successfully.";
            }
            catch (Exception ex)
            {
                ShowError("Unable to remove assignment.", ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            AssignButton.IsEnabled = !busy;
            RemoveButton.IsEnabled = !busy;
            RefreshButton.IsEnabled = !busy;
            LecturerComboBox.IsEnabled = !busy;
            ModuleComboBox.IsEnabled = !busy;
        }

        private void ShowError(string heading, Exception ex)
        {
            string detail = ex switch
            {
                AdminApiException apiEx
                    when apiEx.StatusCode == HttpStatusCode.Conflict =>
                    "The assignment already exists or conflicts with scheduled classes.",

                AdminApiException apiEx
                    when apiEx.StatusCode == HttpStatusCode.Unauthorized ||
                         apiEx.StatusCode == HttpStatusCode.Forbidden =>
                    "Your administrator session is no longer authorized.",

                AdminApiException apiEx
                    when apiEx.StatusCode == HttpStatusCode.BadRequest =>
                    "The selected lecturer or module is invalid.",

                _ => "Please check the API connection and try again."
            };

            StatusText.Text = heading + " " + detail;

            MessageBox.Show(
                heading + Environment.NewLine + detail,
                "CampusPulse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
