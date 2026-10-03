using System;
using System.Windows;
using CampusPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CampusPulse.Admin
{
    public partial class App : System.Windows.Application
    {
        public static CampusPulseDbContext DbContext { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var connectionString = configuration.GetConnectionString("CampusPulseDatabase");

            var options = new DbContextOptionsBuilder<CampusPulseDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            DbContext = new CampusPulseDbContext(options);
        }
    }
}