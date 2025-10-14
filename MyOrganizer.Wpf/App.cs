using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.MVVM;
using MyOrganizer.Wpf.Repository;
using MyOrganizer.Wpf.Services;
using MyOrganizer.Wpf.Services.DB_LocalizationService;



namespace MyOrganizer.Wpf;

public partial class App : Application
{
    public static IHost HostInstance { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        string tt = "";
        HostInstance = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {

                services.AddDbContext<AppDbContext>(opt =>
                {
                    var cs = ctx.Configuration.GetConnectionString("Default")!;
                    tt= cs; 
                    opt.UseSqlServer(cs);
                });

                var rr = tt;
                // register your services from Core/Data here, e.g.:
                // services.AddScoped<IProjectService, ProjectService>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<MainWindow>();
                services.AddTransient<ClientsWindow>();
                services.AddTransient<EditClientWindow>();
                services.AddTransient<ToothWindow>();
                services.AddTransient<IReminderService, ReminderService>();
                services.AddTransient<IToothWorkRepository, ToothWorkRepository>();
                services.AddMemoryCache();
                services.AddScoped<IDbLocalizationService, DbLocalizationService>();
             

            })
            .Build();

        HostInstance.Start();
        //var loc = HostInstance.Services.GetRequiredService<IDbLocalizationService>();
        //loc.WarmUpAsync(AppSettings.CurrentLang).GetAwaiter().GetResult();
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
