using System.Windows;
using HelpDesk_System.Db;
using HelpDesk_System.Services;
using HelpDesk_System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HelpDesk_System;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContextFactory<HelpDeskDbContext>(options =>
                {
                    var connectionString = DatabaseConfiguration.GetRequiredConnectionString(
                        context.Configuration);

                    options.UseNpgsql(connectionString);
                });

                services.AddSingleton<DbInitializer>();
                services.AddSingleton<WindowNavigationService>();
                services.AddSingleton<TicketPriorityCalculator>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<RegisterWindow>();
                services.AddTransient<AuthService>();
                services.AddTransient<TicketService>();
                services.AddTransient<RegistrationRequestService>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var dbInitializer = _host.Services.GetRequiredService<DbInitializer>();
        await dbInitializer.InitializeAsync();

        _host.Services
            .GetRequiredService<LoginWindow>()
            .Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
