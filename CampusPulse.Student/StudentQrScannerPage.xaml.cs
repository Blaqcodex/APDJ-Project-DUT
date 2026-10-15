using CampusPulse.Student.Services;
using Microsoft.Extensions.DependencyInjection;
using ZXing.Net.Maui;

namespace CampusPulse.Student;

public partial class StudentQrScannerPage : ContentPage
{
    private bool hasScanned;
    private bool isSubmitting;
    private bool cameraReady;

    public StudentQrScannerPage()
    {
        InitializeComponent();

        CameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        hasScanned = false;
        cameraReady = false;
        CameraView.IsDetecting = false;

        PermissionStatus permission =
            await Permissions.RequestAsync<Permissions.Camera>();

        if (permission != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Camera permission is required.";
            return;
        }

        cameraReady = true;
        StatusLabel.Text = "Camera ready. Scan a QR code.";
        CameraView.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        cameraReady = false;
        CameraView.IsDetecting = false;
        base.OnDisappearing();
    }

    private void OnBarcodesDetected(
        object? sender,
        BarcodeDetectionEventArgs e)
    {
        if (hasScanned || isSubmitting || e.Results.Length == 0)
        {
            return;
        }

        hasScanned = true;
        string qrValue = e.Results[0].Value;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            CameraView.IsDetecting = false;
            await SubmitAttendanceAsync(qrValue);
        });
    }

    private async Task SubmitAttendanceAsync(string qrValue)
    {
        if (isSubmitting)
        {
            return;
        }

        isSubmitting = true;
        StatusLabel.Text = "Verifying attendance...";

        try
        {
            var services = IPlatformApplication.Current?.Services;

            if (services == null)
            {
                StatusLabel.Text = "Application services unavailable.";
                return;
            }

            var attendanceService =
                services.GetRequiredService<IStudentAttendanceApiService>();

            AttendanceCheckInResult result =
                await attendanceService.CheckInAsync(qrValue);

            StatusLabel.Text = result.Message;

            await DisplayAlertAsync(
                result.Success ? "Attendance Confirmed" : "Check-in Failed",
                result.Message,
                "OK");
        }
        catch (Exception ex) when (
            ex is not OutOfMemoryException)
        {
            StatusLabel.Text =
                "An unexpected error occurred. Please try again.";

            await DisplayAlertAsync(
                "Attendance Error",
                "Unable to complete attendance check-in.",
                "OK");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void OnScanAgainClicked(
        object? sender,
        EventArgs e)
    {
        if (isSubmitting || !cameraReady)
        {
            return;
        }

        hasScanned = false;
        StatusLabel.Text = "Ready to scan another QR code.";
        CameraView.IsDetecting = true;
    }
}