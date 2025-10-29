using System;
using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.Repository;
using MyOrganizer.Wpf.Services;
using MyOrganizer.Wpf.Services.DB_LocalizationService;

namespace MyOrganizer.Wpf
{
    public partial class App : Application
    {
        public static IHost HostInstance { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            HostInstance = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(cfg =>
                {
                    // reloadOnChange keeps JSON updates hot-reloaded during dev
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((ctx, services) =>
                {
                    // Prefer appsettings; let EF_PROVIDER env var override if present
                    var configuredProvider = ctx.Configuration["Database:Provider"] ?? "SqlServer";
                    var envProvider = string.Empty;
                    var provider = string.IsNullOrWhiteSpace(envProvider) ? configuredProvider : envProvider;

                    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        var cs = ctx.Configuration["Database:Sqlite:ConnectionString"]
                                 ?? "Data Source=Data\\MyOrganizerDemo.db";

                        // Normalize to absolute path and ensure directory exists
                        var sb = new SqliteConnectionStringBuilder(cs);
                        var dataSource = sb.DataSource;

                        if (!Path.IsPathRooted(dataSource))
                        {
                            var baseDir = AppContext.BaseDirectory; // bin\Debug\netX\
                            dataSource = Path.Combine(baseDir, dataSource);
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(dataSource)!);
                        sb.DataSource = dataSource;

                        services.AddDbContext<AppDbContext>(opt =>
                            opt.UseSqlite(sb.ToString())
                               .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
                    }
                    else
                    {
                        var cs = ctx.Configuration["Database:SqlServer:ConnectionString"]
                                  ?? ctx.Configuration.GetConnectionString("SqlServer")
                                  ?? "Server=.;Database=My_Organizer;Trusted_Connection=True;TrustServerCertificate=True";
                        

                        services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs).ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
                    }

                    // windows
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<MainWindow>();
                    services.AddTransient<ClientsWindow>();
                    services.AddTransient<EditClientWindow>();
                    services.AddTransient<ToothWindow>();
                    services.AddTransient<TechnicsWindow>();
                    services.AddTransient<ProceduresCatalogWindow>();
                    services.AddTransient<SetPricesDialog>();

                    // repos & services
                    services.AddTransient<IReminderService, ReminderService>();
                    services.AddTransient<IToothWorkRepository, ToothWorkRepository>();
                    services.AddScoped<IDbLocalizationService, DbLocalizationService>();
                    services.AddScoped<IProcedureService, ProcedureService>();
                    services.AddMemoryCache();
                })
                .Build();

            HostInstance.Start();

            using (var scope = HostInstance.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate(); // create/upgrade on start
            }

            AppSettings.CurrentLang ??= "en";
            var loc = HostInstance.Services.GetRequiredService<IDbLocalizationService>();
            _ = loc.WarmUpAsync(AppSettings.CurrentLang);

            var login = HostInstance.Services.GetRequiredService<LoginWindow>();
            login.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (HostInstance is not null) await HostInstance.StopAsync();
            HostInstance?.Dispose();
            base.OnExit(e);
        }
    }
}
