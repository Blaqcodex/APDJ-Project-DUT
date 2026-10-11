using ZXing.Net.Maui;

namespace CampusPulse.Student;

public partial class StudentQrScannerPage : ContentPage
{
    private bool hasScanned;

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

        PermissionStatus permission =
            await Permissions.RequestAsync<Permissions.Camera>();

        if (permission != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Camera permission is required.";
            CameraView.IsDetecting = false;
            return;
        }

        StatusLabel.Text = "Camera ready. Scan a QR code.";
        CameraView.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        CameraView.IsDetecting = false;
        base.OnDisappearing();
    }

    private void OnBarcodesDetected(
        object? sender,
        BarcodeDetectionEventArgs e)
    {
        if (hasScanned || e.Results.Length == 0)
        {
            return;
        }

        hasScanned = true;

        System.Diagnostics.Debug.WriteLine(
            "QR DEBUG: Barcode detected by ZXing.");

        string qrValue = e.Results[0].Value;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            CameraView.IsDetecting = false;
            StatusLabel.Text = "QR code detected.";

            await DisplayAlertAsync(
                "QR Code Scanned",
                "QR code read successfully. Attendance validation will be added next.",
                "OK");
        });
    }

    private void OnScanAgainClicked(
        object? sender,
        EventArgs e)
    {
        hasScanned = false;
        StatusLabel.Text = "Ready to scan another QR code.";
        CameraView.IsDetecting = true;
    }
}


