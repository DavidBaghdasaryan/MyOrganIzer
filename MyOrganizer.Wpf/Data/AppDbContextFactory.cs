using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics; // <-- ВАЖНО
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace MyOrganizer.Wpf.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var cfg = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var provider = Environment.GetEnvironmentVariable("EF_PROVIDER")
                           ?? cfg["Database:Provider"]
                           ?? "SqlServer";

            var options = new DbContextOptionsBuilder<AppDbContext>();

            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // клади файл в папку проекта Data\MyOrganizerDemo.db
                var cs = cfg["Database:Sqlite:ConnectionString"] ?? "Data Source=Data\\MyOrganizerDemo.db";
                options
                    .UseSqlite(cs)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)); // <-- ВАЖНО
            }
            else
            {
                var cs = cfg["Database:SqlServer:ConnectionString"]
                      ?? "Server=.;Database=My_Organizer;Trusted_Connection=True;TrustServerCertificate=True;";
                options
                    .UseSqlServer(cs)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)); // <-- ВАЖНО
            }

            return new AppDbContext(options.Options);
        }
    }
}
