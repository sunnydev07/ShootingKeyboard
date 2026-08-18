using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ShootingKeyboard.Services;
using ShootingKeyboard.ViewModels;

namespace ShootingKeyboard;

/// <summary>
/// Main application class with single-instance guard and DI container
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        // Global exception handling
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"Application Error: {ex?.Message}\n\n{ex?.StackTrace}", "Shooting Keyboard", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Unexpected UI Error: {args.Exception?.Message}\n\n{args.Exception?.StackTrace}", "Shooting Keyboard", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IKeyboardHook, KeyboardHookService>();
        services.AddSingleton<IAudioEngine, AudioEngineService>();
        services.AddSingleton<ISoundPackManager, SoundPackManager>();
        services.AddSingleton<IComboTracker, ComboTracker>();
        services.AddSingleton<IOverlayManager, OverlayManager>();
        services.AddSingleton<ITrayIconManager, TrayIconManager>();
        services.AddSingleton<IStartupManager, StartupManager>();
        services.AddSingleton<IBindingResolver, BindingResolver>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<KeyBindingViewModel>();
        services.AddTransient<SoundPackViewModel>();
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Single-instance guard
        _singleInstanceMutex = new Mutex(true, "ShootingKeyboard_SingleInstance_Mutex", out var createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "Shooting Keyboard is already running.\n\nLook for the icon in your Windows System Tray (near the clock).",
                "Shooting Keyboard",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Initialize services that need early startup
        var configService = _serviceProvider.GetRequiredService<IConfigService>();
        var config = configService.Load();

        var startupManager = _serviceProvider.GetRequiredService<IStartupManager>();
        startupManager.SetStartupEnabled(config.StartWithWindows);

        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        mainViewModel.Initialize();

        // Show the settings & control dashboard window immediately so user sees the app
        mainViewModel.ShowSettingsWindow();

        var tray = _serviceProvider.GetRequiredService<ITrayIconManager>();
        tray.ShowNotification("Shooting Keyboard", "Shooting Keyboard is active! Type anywhere to hear sounds.", BalloonIcon.Info);
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        // Cleanup services
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    public T GetService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
}