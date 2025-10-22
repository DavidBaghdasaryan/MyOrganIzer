using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyOrganizer.Wpf.Data
{
    // Must be public, top-level, and compiled.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var conn = "Server=.;Database=My_Organizer;Trusted_Connection=True;TrustServerCertificate=True;";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(conn)
                .Options;

            return new AppDbContext(options);
        }
    }
}
