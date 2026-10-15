using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QRCoder;

namespace CampusPulse.Admin
{
    public partial class LecturerAttendanceWindow : Window
    {
        private readonly HttpClient _http = new()
        {
            BaseAddress = new Uri("http://localhost:5240/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly DispatcherTimer _timer = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private int? _sessionId;
        private DateTimeOffset _qrExpiresAt;
        private DateTimeOffset _sessionExpiresAt;
        private bool _busy;
        private bool _closingAfterEnd;
        private bool _endingOnClose;

        public LecturerAttendanceWindow()
        {
            InitializeComponent();

            _timer.Tick += Timer_Tick;
        }

        public LecturerAttendanceWindow(int timetableId) : this()
        {
            if (timetableId <= 0)
                throw new ArgumentOutOfRangeException(nameof(timetableId));

            TimetableIdInput.Text = timetableId.ToString();
            TimetableIdInput.IsReadOnly = true;
        }

        private void SetAuthorization()
        {
            if (!LecturerSession.IsAuthenticated)
                throw new InvalidOperationException(
                    "Please sign in as a lecturer.");

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    LecturerSession.Token);
        }

        private async Task<JsonElement> PostAsync(
            string path,
            object? body = null)
        {
            SetAuthorization();

            using var response = await _http.PostAsJsonAsync(
                path,
                body ?? new { });

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Attendance request failed (HTTP {(int)response.StatusCode}).");
            }

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            return document.RootElement.Clone();
        }

        private async Task<JsonElement> GetActiveSessionAsync(
            int timetableId)
        {
            SetAuthorization();

            using var response = await _http.GetAsync(
                $"api/lecturer/attendance/active/{timetableId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Session lookup failed (HTTP {(int)response.StatusCode}).");
            }

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            return document.RootElement.Clone();
        }

        private async Task ResumeAttendanceAsync(int timetableId)
        {
            var active = await GetActiveSessionAsync(timetableId);

            int sessionId = active
                .GetProperty("sessionId")
                .GetInt32();

            DateTimeOffset sessionExpiry = active
                .GetProperty("sessionExpiresAt")
                .GetDateTimeOffset();

            if (sessionExpiry <= DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException(
                    "The previous attendance session has expired. Please try again.");
            }

            var rotated = await PostAsync(
                $"api/lecturer/attendance/{sessionId}/rotate");

            _sessionExpiresAt = sessionExpiry;

            ApplyQrResponse(rotated, false);

            SessionStatusText.Text =
                $"Resumed attendance session #{sessionId}.";

            ErrorText.Text = "";
        }
        private void DisplayQr(string token)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(
                token,
                QRCodeGenerator.ECCLevel.Q);

            var qr = new PngByteQRCode(data);
            byte[] png = qr.GetGraphic(10);

            using var stream = new System.IO.MemoryStream(png);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            QrImage.Source = bitmap;
        }

        private void ApplyQrResponse(
            JsonElement result,
            bool includeSessionExpiry)
        {
            _sessionId = result.GetProperty("sessionId").GetInt32();

            string token =
                result.GetProperty("qrToken").GetString() ?? "";

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException(
                    "API returned an empty QR token.");

            _qrExpiresAt = result
                .GetProperty("qrExpiresAt")
                .GetDateTimeOffset();

            if (includeSessionExpiry)
            {
                _sessionExpiresAt = result
                    .GetProperty("sessionExpiresAt")
                    .GetDateTimeOffset();
            }

            DisplayQr(token);
            RotateButton.IsEnabled = true;
            EndButton.IsEnabled = true;
            StartButton.IsEnabled = false;
            TimetableIdInput.IsEnabled = false;
            DurationInput.IsEnabled = false;

            _timer.Start();
            UpdateStatus();
        }

        private async void Start_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_busy || _sessionId.HasValue)
                return;

            if (!int.TryParse(
                TimetableIdInput.Text,
                out int timetableId) || timetableId <= 0)
            {
                ErrorText.Text = "Enter a valid timetable ID.";
                return;
            }

            if (!int.TryParse(
                DurationInput.Text,
                out int duration) ||
                duration < 5 || duration > 180)
            {
                ErrorText.Text =
                    "Duration must be between 5 and 180 minutes.";
                return;
            }

            _busy = true;
            StartButton.IsEnabled = false;
            ErrorText.Text = "";

            try
            {
                SetAuthorization();

                using var response = await _http.PostAsJsonAsync(
                    "api/lecturer/attendance/start",
                    new
                    {
                        TimetableId = timetableId,
                        DurationMinutes = duration
                    });

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    await ResumeAttendanceAsync(timetableId);
                }
                else if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Attendance request failed (HTTP {(int)response.StatusCode}).");
                }
                else
                {
                    using var document = JsonDocument.Parse(
                        await response.Content.ReadAsStringAsync());

                    ApplyQrResponse(document.RootElement, true);
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = SafeError(ex);
                StartButton.IsEnabled = true;
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task RotateAsync()
        {
            if (_busy || !_sessionId.HasValue)
                return;

            _busy = true;
            RotateButton.IsEnabled = false;

            try
            {
                var result = await PostAsync(
                    $"api/lecturer/attendance/{_sessionId.Value}/rotate");

                ApplyQrResponse(result, false);
                ErrorText.Text = "";
            }
            catch (Exception ex)
            {
                ErrorText.Text = SafeError(ex);
                QrImage.Source = null;
            }
            finally
            {
                _busy = false;
                RotateButton.IsEnabled = _sessionId.HasValue;
            }
        }

        private async void Rotate_Click(
            object sender,
            RoutedEventArgs e)
        {
            await RotateAsync();
        }

        private async void Timer_Tick(
            object? sender,
            EventArgs e)
        {
            UpdateStatus();

            if (_sessionId.HasValue &&
                !_busy &&
                DateTimeOffset.UtcNow >=
                    _qrExpiresAt.AddSeconds(-5) &&
                DateTimeOffset.UtcNow < _sessionExpiresAt)
            {
                await RotateAsync();
            }
        }

        private void UpdateStatus()
        {
            if (!_sessionId.HasValue)
                return;

            var remaining =
                _qrExpiresAt - DateTimeOffset.UtcNow;

            var sessionRemaining =
                _sessionExpiresAt - DateTimeOffset.UtcNow;

            QrStatusText.Text =
                remaining.TotalSeconds > 0
                    ? $"QR refresh in {Math.Ceiling(remaining.TotalSeconds)} seconds"
                    : "QR expired — refreshing...";

            SessionStatusText.Text =
                sessionRemaining.TotalSeconds > 0
                    ? $"Session #{_sessionId} | {Math.Ceiling(sessionRemaining.TotalMinutes)} minutes remaining"
                    : "Attendance session expired.";

            if (sessionRemaining.TotalSeconds <= 0)
            {
                _timer.Stop();
                QrImage.Source = null;
                RotateButton.IsEnabled = false;
                EndButton.IsEnabled = false;
                QrStatusText.Text = "Session expired.";
            }
        }

        private async void End_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_busy || !_sessionId.HasValue)
                return;

            _busy = true;
            EndButton.IsEnabled = false;

            try
            {
                await PostAsync(
                    $"api/lecturer/attendance/{_sessionId.Value}/end");

                _timer.Stop();
                _sessionId = null;
                QrImage.Source = null;

                RotateButton.IsEnabled = false;
                EndButton.IsEnabled = false;
                StartButton.IsEnabled = true;
                TimetableIdInput.IsEnabled = true;
                DurationInput.IsEnabled = true;

                QrStatusText.Text = "Attendance session ended.";
                SessionStatusText.Text = "";
                ErrorText.Text = "";
            }
            catch (Exception ex)
            {
                ErrorText.Text = SafeError(ex);
                EndButton.IsEnabled = true;
            }
            finally
            {
                _busy = false;
            }
        }

        private static string SafeError(Exception ex)
        {
            return ex is HttpRequestException
                ? "Cannot connect to CampusPulse API."
                : ex is TaskCanceledException
                    ? "Attendance request timed out."
                    : ex is InvalidOperationException
                        ? ex.Message
                        : "An unexpected attendance error occurred.";
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (_closingAfterEnd || !_sessionId.HasValue)
            {
                base.OnClosing(e);
                return;
            }

            e.Cancel = true;

            if (_endingOnClose)
                return;

            if (_busy)
            {
                MessageBox.Show(
                    this,
                    "Please wait for the current attendance request to finish.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var confirmation = MessageBox.Show(
                this,
                "An attendance session is still active. End attendance before closing this window?",
                "End Attendance",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;

            _endingOnClose = true;
            _busy = true;
            EndButton.IsEnabled = false;
            RotateButton.IsEnabled = false;
            _timer.Stop();

            try
            {
                await PostAsync(
                    $"api/lecturer/attendance/{_sessionId.Value}/end");

                _sessionId = null;
                QrImage.Source = null;

                _closingAfterEnd = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = SafeError(ex);

                MessageBox.Show(
                    this,
                    "Attendance could not be ended. The window will remain open. Please try again.",
                    "CampusPulse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                EndButton.IsEnabled = true;
                RotateButton.IsEnabled = true;

                if (DateTimeOffset.UtcNow < _sessionExpiresAt)
                    _timer.Start();
            }
            finally
            {
                _busy = false;
                _endingOnClose = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            _http.Dispose();
            base.OnClosed(e);
        }
    }
}