using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using QuickScan.Application.Contracts;
using QuickScan.Application.Services;
using QuickScan.Infrastructure.Scanning;
using QuickScan.Infrastructure.Services;
using QuickScan.UI.Services;
using QuickScan.UI.ViewModels;
using QuickScan.UI.Views;

namespace QuickScan.UI;

public partial class App : Avalonia.Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = _services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core and Infrastructure Services
        services.AddSingleton<IScanEngine, FastParallelScanner>();
        services.AddSingleton<IScanCache, InMemoryScanCache>();
        services.AddSingleton<IDriveService, WindowsDriveService>();
        services.AddSingleton<IFileSystemLauncher, ProcessFileSystemLauncher>();
        services.AddSingleton<ILocalizationService, LocalizationService>();

        // Application Services
        services.AddSingleton<ScanService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
    }
}