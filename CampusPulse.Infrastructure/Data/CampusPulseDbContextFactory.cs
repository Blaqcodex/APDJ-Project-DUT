using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CampusPulse.Infrastructure.Data
{
    public class CampusPulseDbContextFactory : IDesignTimeDbContextFactory<CampusPulseDbContext>
    {
        public CampusPulseDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CampusPulseDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=CampusPulseDb;Trusted_Connection=True;TrustServerCertificate=True;"
            );

            return new CampusPulseDbContext(optionsBuilder.Options);
        }
    }
}