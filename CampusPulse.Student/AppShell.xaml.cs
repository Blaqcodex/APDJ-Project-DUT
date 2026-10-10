
namespace CampusPulse.Student;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(
            nameof(StudentDashboardPage),
            typeof(StudentDashboardPage));

        Routing.RegisterRoute(
            nameof(RegisterPage),
            typeof(RegisterPage));
    }
}
