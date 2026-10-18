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
    public partial class AdminStudentEnrolmentsWindow : Window
    {
        private readonly AdminApiClient _api = new();

        private List<AdminStudentEnrolmentRecord> _students = new();
        private List<AdminProgrammeRecord> _programmes = new();
        private List<AdminModuleRecord> _modules = new();

        private AdminStudentEnrolmentDetail? _detail;

        private bool _loaded;
        private bool _busy;

        public AdminStudentEnrolmentsWindow()
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
                await LoadDataAsync();
            };
        }

        private async Task LoadDataAsync(int? preferredStudentId = null)
        {
            if (_busy)
            {
                return;
            }

            SetBusy(true);
            StatusText.Text = "Loading students and programmes...";

            try
            {
                _students = await _api.GetStudentEnrolmentsAsync();
                _programmes = await _api.GetProgrammesAsync();
                _modules = await _api.GetModulesAsync();

                int? selectedId = preferredStudentId;

                if (!selectedId.HasValue &&
                    StudentSelector.SelectedItem is
                        AdminStudentEnrolmentRecord previous)
                {
                    selectedId = previous.StudentId;
                }

                StudentSelector.ItemsSource = null;
                ProgrammeSelector.ItemsSource = null;

                StudentSelector.ItemsSource = _students;
                ProgrammeSelector.ItemsSource = _programmes;

                if (_students.Count == 0)
                {
                    _detail = null;
                    EnrolledModulesGrid.ItemsSource = null;
                    AvailableModuleSelector.ItemsSource = null;
                    StatusText.Text = "No registered students found.";
                    return;
                }

                StudentSelector.SelectedItem =
                    _students.FirstOrDefault(s =>
                        s.StudentId == selectedId)
                    ?? _students[0];

                await LoadSelectedStudentAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex, "Unable to load enrolment information.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task LoadSelectedStudentAsync()
        {
            if (StudentSelector.SelectedItem is not
                AdminStudentEnrolmentRecord student)
            {
                _detail = null;
                EnrolledModulesGrid.ItemsSource = null;
                AvailableModuleSelector.ItemsSource = null;
                ProgrammeSelector.SelectedItem = null;
                StatusText.Text = "Select a student.";
                return;
            }

            _detail = await _api.GetStudentEnrolmentDetailAsync(
                student.StudentId);

            EnrolledModulesGrid.ItemsSource = _detail.Modules;

            ProgrammeSelector.SelectedItem =
                _programmes.FirstOrDefault(p =>
                    p.ProgrammeId == _detail.ProgrammeId);

            UpdateAvailableModules();

            string programmeName =
                ProgrammeSelector.SelectedItem is AdminProgrammeRecord p
                    ? p.ProgrammeName
                    : "Not assigned";

            StatusText.Text =
                $"{student.FirstName} {student.LastName} | " +
                $"Programme: {programmeName} | " +
                $"Enrolled modules: {_detail.Modules.Count}";
        }

        private void UpdateAvailableModules()
        {
            if (_detail == null || _detail.ProgrammeId <= 0)
            {
                AvailableModuleSelector.ItemsSource = null;
                return;
            }

            var enrolledIds = _detail.Modules
                .Select(m => m.ModuleId)
                .ToHashSet();

            var available = _modules
                .Where(m =>
                    m.ProgrammeId == _detail.ProgrammeId &&
                    !enrolledIds.Contains(m.ModuleId))
                .OrderBy(m => m.ModuleCode)
                .ToList();

            AvailableModuleSelector.ItemsSource = available;

            if (available.Count > 0)
            {
                AvailableModuleSelector.SelectedIndex = 0;
            }
        }

        private async void StudentSelector_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_loaded || _busy ||
                StudentSelector.SelectedItem == null)
            {
                return;
            }

            SetBusy(true);

            try
            {
                await LoadSelectedStudentAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex, "Unable to load the selected student.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void AssignProgramme_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_busy)
            {
                return;
            }

            if (StudentSelector.SelectedItem is not
                    AdminStudentEnrolmentRecord student ||
                ProgrammeSelector.SelectedItem is not
                    AdminProgrammeRecord programme)
            {
                MessageBox.Show(
                    "Select a student and programme first.",
                    "Student Enrolments",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_detail != null &&
                _detail.ProgrammeId == programme.ProgrammeId)
            {
                MessageBox.Show(
                    "This student already has the selected programme. " +
                    "Use the module controls to adjust individual enrolments.",
                    "Student Enrolments",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var confirmation = MessageBox.Show(
                $"Assign {programme.ProgrammeName} to " +
                $"{student.FirstName} {student.LastName}?\n\n" +
                "All modules in the selected programme will be enrolled. " +
                "Existing module enrolments will be preserved.",
                "Confirm Programme Assignment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            SetBusy(true);

            try
            {
                await _api.AssignStudentProgrammeAsync(
                    student.StudentId,
                    programme.ProgrammeId);

                SetBusy(false);
                await LoadDataAsync(student.StudentId);

                MessageBox.Show(
                    "Programme assigned and its modules enrolled.",
                    "Student Enrolments",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex, "Unable to assign the programme.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void AddModule_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_busy)
            {
                return;
            }

            if (StudentSelector.SelectedItem is not
                    AdminStudentEnrolmentRecord student ||
                AvailableModuleSelector.SelectedItem is not
                    AdminModuleRecord module)
            {
                MessageBox.Show(
                    "Select a student and an available module.",
                    "Student Enrolments",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            SetBusy(true);

            try
            {
                await _api.AddStudentModuleAsync(
                    student.StudentId,
                    module.ModuleId);

                await LoadSelectedStudentAsync();

                MessageBox.Show(
                    "Student enrolled in the selected module.",
                    "Student Enrolments",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex, "Unable to add the module.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void RemoveModule_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_busy)
            {
                return;
            }

            if (StudentSelector.SelectedItem is not
                    AdminStudentEnrolmentRecord student ||
                EnrolledModulesGrid.SelectedItem is not
                    AdminStudentEnrolledModule module)
            {
                MessageBox.Show(
                    "Select an enrolled module to remove.",
                    "Student Enrolments",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var confirmation = MessageBox.Show(
                $"Remove {module.ModuleCode} from " +
                $"{student.FirstName} {student.LastName}?\n\n" +
                "Historical attendance records will be preserved.",
                "Confirm Module Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            SetBusy(true);

            try
            {
                await _api.RemoveStudentModuleAsync(
                    student.StudentId,
                    module.ModuleId);

                await LoadSelectedStudentAsync();

                MessageBox.Show(
                    "Module enrolment removed.",
                    "Student Enrolments",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex, "Unable to remove the module.");
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
            if (_busy)
            {
                return;
            }

            await LoadDataAsync();
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;

            StudentSelector.IsEnabled = !busy;
            ProgrammeSelector.IsEnabled = !busy;
            AssignProgrammeButton.IsEnabled = !busy;
            AvailableModuleSelector.IsEnabled = !busy;
            AddModuleButton.IsEnabled = !busy;
            RemoveModuleButton.IsEnabled = !busy;
            RefreshButton.IsEnabled = !busy;
        }

        private static string GetErrorMessage(
            Exception exception,
            string fallback)
        {
            if (exception is AdminApiException apiException)
            {
                return apiException.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or
                    HttpStatusCode.Forbidden =>
                        "Administrator session is no longer authorized.",

                    HttpStatusCode.BadRequest =>
                        "The requested enrolment is invalid.",

                    HttpStatusCode.NotFound =>
                        "The selected student, programme or module was not found.",

                    HttpStatusCode.Conflict =>
                        "The requested enrolment conflicts with an existing record.",

                    _ => fallback
                };
            }

            if (exception is HttpRequestException)
            {
                return "Cannot connect to the CampusPulse API.";
            }

            if (exception is TaskCanceledException)
            {
                return "The API request timed out.";
            }

            return fallback;
        }

        private void ShowError(Exception exception, string fallback)
        {
            string message = GetErrorMessage(exception, fallback);

            StatusText.Text = message;

            MessageBox.Show(
                message,
                "Student Enrolments",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
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