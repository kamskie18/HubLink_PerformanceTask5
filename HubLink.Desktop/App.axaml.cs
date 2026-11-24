using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HubLink.Application.Configuration;
using HubLink.Application.Services;
using HubLink.Desktop.ViewModels;
using HubLink.Desktop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace HubLink.Desktop;

public partial class App : Avalonia.Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = configBuilder.Build();

        services.Configure<HubLinkOptions>(options =>
        {
            configuration.GetSection(HubLinkOptions.SectionName).Bind(options);
        });

        // HTTP Client & API Service
        services.AddHttpClient<IHubLinkApiClient, HubLinkApiClient>();

        // CSV Export Service
        services.AddSingleton<ICsvExportService, CsvExportService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
    }
}
