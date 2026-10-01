using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.Admin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TestDatabaseConnection();
        }

        private void TestDatabaseConnection()
        {
            try
            {
                using var db = App.DbContext;

                if (db.Database.CanConnect())
                {
                    MessageBox.Show(
                        "CampusPulse successfully connected to SQL Server!",
                        "Database Connection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "CampusPulse could not connect to SQL Server.",
                        "Database Connection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Database connection failed:\n\n" + ex.Message,
                    "Database Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}