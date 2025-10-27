using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace MyOrganizer.Wpf.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Load appsettings (optional but handy)
            var cfg = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var provider = Environment.GetEnvironmentVariable("EF_PROVIDER")
                           ?? cfg["EF_PROVIDER"]
                           ?? "SqlServer"; // default

            var options = new DbContextOptionsBuilder<AppDbContext>();

            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // use your SQLite conn string (or fallback)
                var cs = cfg.GetConnectionString("Sqlite") ?? "Data Source=app.db";
                options.UseSqlite(cs);
            }
            else
            {
                // SQL Server default
                var cs = cfg.GetConnectionString("Default")
                         ?? "Server=.;Database=My_Organizer;Trusted_Connection=True;TrustServerCertificate=True;";
                options.UseSqlServer(cs);
            }

            return new AppDbContext(options.Options);
        }
    }
}
