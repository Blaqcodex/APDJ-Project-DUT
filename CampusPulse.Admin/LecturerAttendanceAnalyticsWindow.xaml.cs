using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace CampusPulse.Admin
{
    public partial class LecturerAttendanceAnalyticsWindow : Window
    {
        private static readonly HttpClient Http = new()
        {
            BaseAddress = new Uri("http://localhost:5240/")
        };

        private readonly int _moduleId;

        private List<StudentAttendanceRow>? _exportStudents;
        private string _exportModuleCode = "";
        private string _exportModuleName = "";
        private int _exportEnrolled;
        private int _exportCompleted;
        private int _exportPending;
        private int _exportPresent;
        private double? _exportPercentage;


        public LecturerAttendanceAnalyticsWindow(int moduleId)
        {
            InitializeComponent();

            _moduleId = moduleId;

            Loaded += async (_, _) => await LoadAnalyticsAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAnalyticsAsync();
        }

        private async Task LoadAnalyticsAsync()
        {
            RefreshButton.IsEnabled = false;
            ExportExcelButton.IsEnabled = false;
            _exportStudents = null;
            StatusText.Text = "Loading attendance analytics...";

            try
            {
                if (string.IsNullOrWhiteSpace(LecturerSession.Token))
                {
                    throw new InvalidOperationException(
                        "Your lecturer session has expired. Please sign in again.");
                }

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/lecturer/attendance-analytics/modules/{_moduleId}");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        LecturerSession.Token);

                using var response = await Http.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException(
                        "Your session is no longer valid. Please sign in again.");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException(
                        "You are not assigned to this module.");
                }

                response.EnsureSuccessStatusCode();

                using var document = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync());

                var root = document.RootElement;

                string moduleCode =
                    root.GetProperty("moduleCode").GetString() ?? "";

                string moduleName =
                    root.GetProperty("moduleName").GetString() ?? "";

                int enrolled =
                    root.GetProperty("enrolledStudents").GetInt32();

                int completed =
                    root.GetProperty("completedSessions").GetInt32();

                int pending =
                    root.GetProperty("pendingSessions").GetInt32();

                int present =
                    root.GetProperty("totalPresent").GetInt32();

                var percentageElement =
                    root.GetProperty("attendancePercentage");

                string percentage =
                    percentageElement.ValueKind == JsonValueKind.Null
                        ? "N/A"
                        : percentageElement.GetDouble().ToString("0.0") + "%";

                var students = new List<StudentAttendanceRow>();

                foreach (var item in root.GetProperty("students").EnumerateArray())
                {
                    int studentPresent =
                        item.GetProperty("present").GetInt32();

                    int studentAbsent =
                        item.GetProperty("absent").GetInt32();

                    var studentPercentage =
                        item.GetProperty("attendancePercentage");

                    double? rate =
                        studentPercentage.ValueKind == JsonValueKind.Null
                            ? null
                            : studentPercentage.GetDouble();

                    students.Add(new StudentAttendanceRow
                    {
                        StudentNumber =
                            item.GetProperty("studentNumber").GetString() ?? "",

                        FullName =
                            (item.GetProperty("firstName").GetString() ?? "") +
                            " " +
                            (item.GetProperty("lastName").GetString() ?? ""),

                        Present = studentPresent,
                        Absent = studentAbsent,
                        AttendancePercentage = rate
                    });
                }

                ModuleTitleText.Text = $"{moduleCode} — {moduleName}";
                StudentsCountText.Text = enrolled.ToString();
                SessionsCountText.Text = completed.ToString();
                AttendancePercentageText.Text = percentage;
                PresentMarksText.Text = present.ToString();

                StudentsGrid.ItemsSource = students;

                _exportModuleCode = moduleCode;
                _exportModuleName = moduleName;
                _exportEnrolled = enrolled;
                _exportCompleted = completed;
                _exportPending = pending;
                _exportPresent = present;
                _exportPercentage =
                    percentageElement.ValueKind == JsonValueKind.Null
                        ? null
                        : percentageElement.GetDouble();

                _exportStudents = students;
                ExportExcelButton.IsEnabled = true;

                StatusText.Text =
                    $"Loaded {students.Count} students; {pending} pending sessions.";

                FooterText.Text =
                    "*Absent means no Present check-in was recorded for a " +
                    "completed session. Percentages use current enrollment, " +
                    "not historical enrollment dates. " +
                    "Review records before contacting students.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Unable to load analytics.";

                MessageBox.Show(
                    ex.Message,
                    "Attendance Analytics",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_exportStudents == null || !ExportExcelButton.IsEnabled)
            {
                MessageBox.Show(
                    "Load attendance analytics before exporting.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Save CampusPulse Attendance Report",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName =
                    $"CampusPulse_Attendance_{_exportModuleCode}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();

                var sheet = workbook.Worksheets.Add("Attendance Report");

                sheet.Cell("A1").Value = "CAMPUSPULSE";
                sheet.Range("A1:F1").Merge();
                sheet.Range("A1:F1").Style.Font.Bold = true;
                sheet.Range("A1:F1").Style.Font.FontSize = 20;
                sheet.Range("A1:F1").Style.Font.FontColor = XLColor.White;
                sheet.Range("A1:F1").Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#172B4D");
                sheet.Row(1).Height = 36;

                sheet.Cell("A2").Value = "Lecturer Attendance Report";
                sheet.Range("A2:F2").Merge();
                sheet.Range("A2:F2").Style.Font.Bold = true;
                sheet.Range("A2:F2").Style.Font.FontSize = 14;

                sheet.Cell("A4").Value = "Module";
                sheet.Cell("B4").Value =
                    $"{_exportModuleCode} - {_exportModuleName}";

                sheet.Cell("A5").Value = "Generated";
                sheet.Cell("B5").Value = DateTime.Now;
                sheet.Cell("B5").Style.DateFormat.Format =
                    "yyyy-mm-dd hh:mm";

                sheet.Cell("A6").Value = "Enrollment basis";
                sheet.Cell("B6").Value = "Current module enrollments";

                sheet.Cell("A8").Value = "Enrolled students";
                sheet.Cell("B8").Value = _exportEnrolled;

                sheet.Cell("C8").Value = "Completed sessions";
                sheet.Cell("D8").Value = _exportCompleted;

                sheet.Cell("E8").Value = "Pending sessions";
                sheet.Cell("F8").Value = _exportPending;

                sheet.Cell("A9").Value = "Present marks";
                sheet.Cell("B9").Value = _exportPresent;

                sheet.Cell("C9").Value = "Overall attendance";

                if (_exportPercentage.HasValue)
                {
                    sheet.Cell("D9").Value =
                        _exportPercentage.Value / 100.0;

                    sheet.Cell("D9").Style.NumberFormat.Format = "0.0%";
                }
                else
                {
                    sheet.Cell("D9").Value = "N/A";
                }

                var summaryLabels = sheet.Range("A8:F9");
                summaryLabels.Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#EAF2F8");

                sheet.Cell("A12").Value = "Student Number";
                sheet.Cell("B12").Value = "Student";
                sheet.Cell("C12").Value = "Present";
                sheet.Cell("D12").Value = "No Check-in Recorded";
                sheet.Cell("E12").Value = "Attendance %";
                sheet.Cell("F12").Value = "Follow-up";

                var header = sheet.Range("A12:F12");
                header.Style.Font.Bold = true;
                header.Style.Font.FontColor = XLColor.White;
                header.Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#0F766E");

                int row = 13;

                foreach (var student in _exportStudents)
                {
                    sheet.Cell(row, 1).SetValue(student.StudentNumber);
                    sheet.Cell(row, 1).Style.NumberFormat.Format = "@";

                    sheet.Cell(row, 2).Value = student.FullName;
                    sheet.Cell(row, 3).Value = student.Present;
                    sheet.Cell(row, 4).Value = student.Absent;

                    if (student.AttendancePercentage.HasValue)
                    {
                        sheet.Cell(row, 5).Value =
                            student.AttendancePercentage.Value / 100.0;

                        sheet.Cell(row, 5).Style.NumberFormat.Format =
                            "0.0%";
                    }
                    else
                    {
                        sheet.Cell(row, 5).Value = "N/A";
                    }

                    sheet.Cell(row, 6).Value = student.SupportStatus;

                    if (student.AttendancePercentage.HasValue &&
                        student.AttendancePercentage.Value < 75)
                    {
                        sheet.Range(row, 1, row, 6)
                            .Style.Fill.BackgroundColor =
                                XLColor.FromHtml("#FFF1E6");
                    }
                    else if (row % 2 == 0)
                    {
                        sheet.Range(row, 1, row, 6)
                            .Style.Fill.BackgroundColor =
                                XLColor.FromHtml("#F8FAFC");
                    }

                    row++;
                }

                int lastStudentRow = Math.Max(12, row - 1);

                sheet.Range(12, 1, lastStudentRow, 6)
                    .SetAutoFilter();

                sheet.SheetView.FreezeRows(12);

                int noteRow = row + 2;

                sheet.Cell(noteRow, 1).Value = "REPORTING NOTES";
                sheet.Range(noteRow, 1, noteRow, 6).Merge();
                sheet.Cell(noteRow, 1).Style.Font.Bold = true;

                sheet.Cell(noteRow + 1, 1).Value =
                    "Attendance percentages use current module enrollments and completed sessions.";

                sheet.Cell(noteRow + 2, 1).Value =
                    "No check-in recorded does not confirm that a student was physically absent.";

                sheet.Cell(noteRow + 3, 1).Value =
                    "Pending sessions are excluded from attendance percentages.";

                sheet.Cell(noteRow + 4, 1).Value =
                    "Follow-up is flagged below 75%; review records before contacting students.";

                sheet.Cell(noteRow + 5, 1).Value =
                    "This report contains student information. Store and share it securely.";

                for (int i = 1; i <= 5; i++)
                {
                    sheet.Range(noteRow + i, 1, noteRow + i, 6).Merge();
                }

                sheet.Columns("A:F").AdjustToContents();

                sheet.Column("A").Width =
                    Math.Max(sheet.Column("A").Width, 19);

                sheet.Column("B").Width =
                    Math.Max(sheet.Column("B").Width, 26);

                sheet.Column("D").Width =
                    Math.Max(sheet.Column("D").Width, 23);

                sheet.Column("F").Width =
                    Math.Max(sheet.Column("F").Width, 23);

                sheet.Column("B").Width =
                    Math.Min(sheet.Column("B").Width, 55);

                workbook.SaveAs(dialog.FileName);

                MessageBox.Show(
                    "Attendance report saved successfully.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to export attendance report: " + ex.Message,
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        public sealed class StudentAttendanceRow
        {
            public string StudentNumber { get; set; } = "";
            public string FullName { get; set; } = "";
            public int Present { get; set; }
            public int Absent { get; set; }
            public double? AttendancePercentage { get; set; }

            public string PercentageDisplay =>
                AttendancePercentage.HasValue
                    ? AttendancePercentage.Value.ToString("0.0") + "%"
                    : "N/A";

            public string SupportStatus =>
                !AttendancePercentage.HasValue
                    ? "Not enough data"
                    : AttendancePercentage.Value < 75
                        ? "Review attendance"
                        : "No follow-up flagged";
        }
    }
}