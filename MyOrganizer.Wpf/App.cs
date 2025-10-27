using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.Repository;
using MyOrganizer.Wpf.Services;
using MyOrganizer.Wpf.Services.DB_LocalizationService;

namespace MyOrganizer.Wpf;

public partial class App : Application
{
    public static IHost HostInstance { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        HostInstance = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                // Host.CreateDefaultBuilder already loads appsettings.json + env vars,
                // but this keeps your "reloadOnChange" behavior:
                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var provider = Environment.GetEnvironmentVariable("EF_PROVIDER")
                                ?? ctx.Configuration.GetValue<string>("Database:Provider")
                                ?? "SqlServer";

                services.AddDbContext<AppDbContext>(opt =>
                {
                    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        var sqliteConn = ctx.Configuration.GetConnectionString("Sqlite")
                                         ?? "Data Source=MyOrganizerDemo.db";
                        opt.UseSqlite(sqliteConn);
                    }
                    else
                    {
                        var sqlConn = ctx.Configuration.GetConnectionString("SqlServer")
                                      ?? "Server=.;Database=My_Organizer;Trusted_Connection=True;TrustServerCertificate=True";
                        opt.UseSqlServer(sqlConn);
                    }
                });

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
            db.Database.Migrate(); // auto-create/upgrade on start (works for SqlServer and Sqlite)
        }

        AppSettings.CurrentLang ??= "en";
        var loc = HostInstance.Services.GetRequiredService<IDbLocalizationService>();
        loc.WarmUpAsync(AppSettings.CurrentLang);

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
