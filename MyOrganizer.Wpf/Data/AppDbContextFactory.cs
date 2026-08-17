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
            var projectDir = Path.GetDirectoryName(typeof(AppDbContextFactory).Assembly.Location);
            var basePath = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Migrations"))
                ? Directory.GetCurrentDirectory()
                : (projectDir is not null && File.Exists(Path.Combine(projectDir, "appsettings.json"))
                    ? projectDir
                    : Directory.GetCurrentDirectory());

            // Design-time: prefer the project folder so appsettings.json is found.
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "appsettings.json")))
                    dir = dir.Parent;
                if (dir is not null)
                    basePath = dir.FullName;
            }

            var cfg = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var provider = Environment.GetEnvironmentVariable("EF_PROVIDER")
                           ?? cfg["Database:Provider"]
                           ?? "SqlServer";

            var options = new DbContextOptionsBuilder<AppDbContext>();

            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var cs = cfg["Database:Sqlite:ConnectionString"] ?? "Data Source=Data\\MyOrganizerDemo.db";
                options.UseSqlite(cs);
            }
            else
            {
                var cs = cfg["Database:SqlServer:ConnectionString"]
                      ?? "Server=.;Database=My_Organizer;Trusted_Connection=True;TrustServerCertificate=True;";
                options.UseSqlServer(cs);
            }

            return new AppDbContext(options.Options);
        }
    }
}
