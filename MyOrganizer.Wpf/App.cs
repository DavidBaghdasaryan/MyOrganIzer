using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.MVVM.ViewModels;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Repository;
using MyOrganizer.Wpf.Services;
using MyOrganizer.Wpf.Services.DB_LocalizationService;

namespace MyOrganizer.Wpf;

public partial class App : Application
{
    public static IHost HostInstance { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Any(a => string.Equals(a, "--generate-odontogram-thumbs", StringComparison.OrdinalIgnoreCase)))
        {
            var dir = Controls.OdontogramThumbRenderer.GenerateAll();
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "odontogram-thumbs.txt"), dir); }
            catch { /* generate report is optional */ }
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi48-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi48SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi38-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi38SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi28-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi28SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi18-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi18SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi47-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi47SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi37-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi37SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi27-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi27SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi17-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi17SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi11-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi11SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi21-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi21SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi12-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi12SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi22-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi22SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi31-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi31SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi41-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi41SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi32-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi32SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi42-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi42SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi13-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi13SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi23-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi23SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi33-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi33SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi43-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi43SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi14-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi14SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi15-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi15SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi25-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi25SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi24-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi24SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi34-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi34SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi35-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi35SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi45-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi45SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi44-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi44SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi16-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi16SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi26-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi26SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi36-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi16SurfaceMapStore.DumpTopology();
            Controls.Fdi36SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--generate-fdi46-map", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi46SurfaceMapStore.GenerateDefault();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--patch-fdi16-cej-red", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.Fdi16SurfaceMapStore.PatchCejRedContinuity();
            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--diagnose-cervical-seam", StringComparison.OrdinalIgnoreCase)))
        {
            Controls.CervicalSeamProbe.DiagnoseFrozen36And46();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppSettings.Load();

        HostInstance = Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var configuredProvider = ctx.Configuration["Database:Provider"] ?? "Sqlite";
                var envProvider = Environment.GetEnvironmentVariable("EF_PROVIDER");
                var provider = string.IsNullOrWhiteSpace(envProvider) ? configuredProvider : envProvider;

                void ConfigureDb(DbContextOptionsBuilder opt) =>
                    opt.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

                if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    var cs = ctx.Configuration["Database:Sqlite:ConnectionString"]
                             ?? "Data Source=MyOrganizer.db";
                    var sb = new SqliteConnectionStringBuilder(cs);
                    sb.DataSource = ResolveSqlitePath(sb.DataSource);

                    services.AddDbContext<AppDbContext>(opt =>
                    {
                        ConfigureDb(opt);
                        opt.UseSqlite(sb.ToString());
                    });
                }
                else
                {
                    var cs = ctx.Configuration["Database:SqlServer:ConnectionString"]
                              ?? ctx.Configuration.GetConnectionString("SqlServer")
                              ?? "Server=.;Database=My_Organizer;Trusted_Connection=True;TrustServerCertificate=True";

                    services.AddDbContext<AppDbContext>(opt =>
                    {
                        ConfigureDb(opt);
                        opt.UseSqlServer(cs);
                    });
                }

                services.AddTransient<LoginWindow>();
                services.AddTransient<MainWindow>();
                services.AddTransient<ClientsWindow>();
                services.AddTransient<EditClientWindow>();
                services.AddTransient<ToothWindow>();
                services.AddTransient<TechnicsWindow>();
                services.AddTransient<ProceduresCatalogWindow>();
                services.AddTransient<SetPricesDialog>();

                services.AddScoped<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddScoped<ILegacyWindowService, LegacyWindowService>();
                services.AddScoped<ShellViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddScoped<ClientsViewModel>();
                services.AddTransient<ClientWorkspaceViewModel>();
                services.AddTransient<DentalChartViewModel>();
                services.AddScoped<ProceduresViewModel>();
                services.AddScoped<TechniciansViewModel>();
                services.AddTransient<ToothLabViewModel>();

                services.AddTransient<IReminderService, ReminderService>();
                services.AddTransient<IToothWorkRepository, ToothWorkRepository>();
                services.AddSingleton<IDbLocalizationService, DbLocalizationService>();
                services.AddScoped<IProcedureService, ProcedureService>();
                services.AddMemoryCache();
            })
            .Build();

        HostInstance.Start();

        try
        {
            using var scope = HostInstance.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            var procedures = scope.ServiceProvider.GetRequiredService<IProcedureService>();
            procedures.EnsureDefaultPrices();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Database", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        try
        {
            var loc = HostInstance.Services.GetRequiredService<IDbLocalizationService>();
            loc.WarmUpAsync(AppSettings.CurrentLang).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Localization", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        var login = HostInstance.Services.GetRequiredService<LoginWindow>();
        login.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (HostInstance is not null)
            await HostInstance.StopAsync();
        HostInstance?.Dispose();
        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        try
        {
            ModernDialog.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string ResolveSqlitePath(string dataSource)
    {
        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyOrganizer");
        Directory.CreateDirectory(appDir);

        if (!Path.IsPathRooted(dataSource))
            dataSource = Path.Combine(appDir, Path.GetFileName(dataSource));

        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var legacy = Path.Combine(AppContext.BaseDirectory, "Data", "MyOrganizerDemo.db");
        if (!File.Exists(dataSource) && File.Exists(legacy))
            File.Copy(legacy, dataSource);

        return dataSource;
    }
}
