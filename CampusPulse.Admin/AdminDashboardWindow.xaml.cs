using System.Windows;

namespace CampusPulse.Admin
{
    public partial class AdminDashboardWindow : Window
    {
        public AdminDashboardWindow()
        {
            if (!AdminSession.IsAuthenticated)
            {
                throw new System.InvalidOperationException(
                    "Administrator authentication is required.");
            }

            InitializeComponent();

            WelcomeText.Text =
                $"Welcome, {AdminSession.DisplayName} | Role: Admin";
        }


        private void OpenUsers_Click(
            object sender,
            RoutedEventArgs e)
        {
            var users = new AdminUsersWindow();

            System.Windows.Application.Current.MainWindow = users;
            users.Show();

            Close();
        }
        private void OpenModules_Click(object sender, RoutedEventArgs e)
        {
            var modules = new AdminModulesWindow();

            System.Windows.Application.Current.MainWindow = modules;
            modules.Show();

            Close();
        }
        private void OpenProgrammes_Click(object sender, RoutedEventArgs e)
        {
            var programmes = new AdminProgrammesWindow();

            System.Windows.Application.Current.MainWindow = programmes;
            programmes.Show();

            Close();
        }
        private void OpenLecturerAssignments_Click(
            object sender,
            RoutedEventArgs e)
        {
            var assignments = new AdminLecturerAssignmentsWindow();

            System.Windows.Application.Current.MainWindow = assignments;
            assignments.Show();

            Close();
        }
        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            AdminSession.SignOut();

            var selection = new PortalSelectionWindow();
            System.Windows.Application.Current.MainWindow = selection;
            selection.Show();

            Close();
        }
    }
}