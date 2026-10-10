
using CampusPulse.Student.Services;
using Microsoft.Extensions.Logging;

namespace CampusPulse.Student;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register the student login screen.
        builder.Services.AddTransient<MainPage>();

        // Register the authentication API service.
        builder.Services.AddHttpClient<
            IAuthenticationApiService,
            AuthenticationApiService>(client =>
            {
#if DEBUG
                // Android emulator connects to the Windows host.
                client.BaseAddress = new Uri(
                    "http://10.0.2.2:5240/");
#else
            // Replace with the deployed HTTPS API address before release.
            client.BaseAddress = new Uri(
                "https://api.example.com/");
#endif

                client.Timeout = TimeSpan.FromSeconds(15);
            });

        // Register secure authentication session storage.
        builder.Services.AddSingleton<
            ISessionService,
            SessionService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
