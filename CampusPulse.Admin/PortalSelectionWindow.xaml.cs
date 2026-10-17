using System.Windows;

namespace CampusPulse.Admin
{
    public partial class PortalSelectionWindow : Window
    {
        public PortalSelectionWindow()
        {
            InitializeComponent();
        }

        private void Lecturer_Click(object sender, RoutedEventArgs e)
        {
            var login = new LecturerLoginWindow();

            System.Windows.Application.Current.MainWindow = login;
            login.Show();

            Close();
        }

        private void Admin_Click(object sender, RoutedEventArgs e)
        {
            var login = new AdminLoginWindow();

            System.Windows.Application.Current.MainWindow = login;
            login.Show();

            Close();
        }
    }
}