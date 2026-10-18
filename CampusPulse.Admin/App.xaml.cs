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

        public static Uri ApiBaseAddress { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
                .Build();

            var apiBaseUrl = configuration["ApiBaseUrl"];

            if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri) ||
                (apiUri.Scheme != Uri.UriSchemeHttp &&
                 apiUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    "A valid ApiBaseUrl must be configured.");
            }

            ApiBaseAddress = apiUri;

            var connectionString = configuration.GetConnectionString("CampusPulseDatabase");

            var options = new DbContextOptionsBuilder<CampusPulseDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            DbContext = new CampusPulseDbContext(options);
        }
    }
}