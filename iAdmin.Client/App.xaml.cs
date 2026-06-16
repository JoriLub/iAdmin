using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using iAdmin.Client.Services;
using iAdmin.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace iAdmin.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    
    public IServiceProvider? Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Serilog configuration
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "iAdmin", "Logs");
        Directory.CreateDirectory(appDataPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(appDataPath, "client-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // Setup DI container
        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient<IApiClient, ApiClient>()
                    .ConfigureHttpClient(client =>
                    {
                        client.BaseAddress = new Uri("http://localhost:5000");
                        client.Timeout = TimeSpan.FromSeconds(30);
                    });

                services.AddSingleton<ITokenService, TokenService>();
                services.AddSingleton<LoginViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        Services = _host.Services;
        _host.Start();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.StopAsync().Wait();
        _host?.Dispose();
        base.OnExit(e);
    }
}

