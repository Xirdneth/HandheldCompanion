using ConnectingApps.SmartInject;
using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Devices;
using HandheldCompanion.Helpers;
using HandheldCompanion.Managers;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Misc;
using HandheldCompanion.Platforms;
using HandheldCompanion.Processors;
using HandheldCompanion.UI;
using HandheldCompanion.Utils;
using HandheldCompanion.Views;
using HandheldCompanion.Views.Pages;
using HandheldCompanion.Views.Pages.Interfaces;
using HandheldCompanion.Views.QuickPages;
using HandheldCompanion.Views.QuickPages.Interfaces;
using HandheldCompanion.Views.Windows;
using HandheldCompanion.Views.Windows.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using static HandheldCompanion.WinAPI;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace HandheldCompanion;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    private IHost _host;

    private Assembly? CurrentAssembly;
    private FileVersionInfo? fileVersionInfo;
    private string? currentCulture;
    private CultureInfo? cultureInfo;
    public static IServiceProvider ServiceProvider;

    public App()
    {
        InitializeComponent();

        CurrentAssembly = Assembly.GetExecutingAssembly();
        fileVersionInfo = FileVersionInfo.GetVersionInfo(CurrentAssembly.Location);
       
        cultureInfo = CultureInfo.CurrentCulture;

        _host = new HostBuilder()
            .ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                configurationBuilder.SetBasePath(context.HostingEnvironment.ContentRootPath);
                configurationBuilder.AddJsonFile("HandheldCompanion.json", optional: false);
            })
            .ConfigureServices((context, services) =>
            {
                //Managers
                services.AddSingleton<MainWindow>();               
                services.AddLazyScoped<IGPUManager,GPUManager>();
                services.AddLazyScoped<IPowerProfileManager, PowerProfileManager>();
                services.AddLazyScoped<IProfileManager, ProfileManager>();
                services.AddLazyScoped<IPlatformManager, PlatformManager>();
                services.AddLazyScoped<IPerformanceManager, PerformanceManager>();
                services.AddLazyScoped<ISettingsManager, SettingsManager>();
                services.AddLazyScoped<ILayoutManager, LayoutManager>();
                services.AddLazyScoped<IMotionManager, MotionManager>();
                services.AddLazyScoped<IOSDManager, OSDManager>();
                services.AddLazyScoped<ISensorsManager, SensorsManager>();
                services.AddLazyScoped<IControllerManager, ControllerManager>();
                services.AddLazyScoped<IDynamicLightingManager, DynamicLightingManager>();
                services.AddLazyScoped<IMultimediaManager, MultimediaManager>();
                services.AddLazyScoped<IVirtualManager, VirtualManager>();
                services.AddLazyScoped<IInputsManager, InputsManager>();
                services.AddLazyScoped<IProcessManager, ProcessManager>();
                services.AddLazyScoped<ITaskManager, TaskManager>();
                services.AddLazyScoped<IUpdateManager, UpdateManager>();
                services.AddLazyScoped<IDeviceManager, DeviceManager>();
                services.AddLazyScoped<ISystemManager, SystemManager>();
                services.AddLazyScoped<IToastManager, ToastManager>();
                services.AddLazyScoped<ITimerManager, TimerManager>();
                services.AddLazyScoped<IHotkeysManager, HotkeysManager>();

                //Models
                services.AddLazySingleton<IRTSS, RTSS>();
                services.AddLazySingleton<IXInputPlus, XInputPlus>();
                services.AddLazySingleton<IUISounds, UISounds>();
                services.AddLazySingleton<IPowerProfile, PowerProfile>();
                services.AddLazySingleton<IIDevice, IDevice>();
                services.AddLazySingleton<ILegionGo, LegionGo>();
                services.AddLazySingleton<IXInputController, XInputController>();
                services.AddLazySingleton<IVangoghGPU, VangoghGPU>();
                services.AddSingleton<IProcessor, Processor>();

                //Windows
                services.AddSingleton<IViewModel, ViewModel>();
                services.AddLazySingleton<IOverlayModel, OverlayModel>();
                services.AddLazySingleton<IOverlayQuickTools, OverlayQuickTools>();
                services.AddLazySingleton<IOverlayTrackpad, OverlayTrackpad>();

                //Pages
                services.AddLazySingleton<IOverlayPage, OverlayPage>();
                services.AddLazySingleton<IILayoutPage, LayoutPage>();
                services.AddLazySingleton<IProfilesPage, ProfilesPage>();
                services.AddLazySingleton<IControllerPage, ControllerPage>();
                services.AddLazySingleton<INotificationsPage, NotificationsPage>();
                services.AddLazySingleton<IHotkeysPage, HotkeysPage>();
                services.AddLazySingleton<IAboutPage, AboutPage>();
                services.AddLazySingleton<IDevicePage, DevicePage>();
                services.AddLazySingleton<ISettingsPage, SettingsPage>();
                services.AddLazySingleton<IPerformancePage, PerformancePage>();

                //Quick Pages
                services.AddLazySingleton<IQuickHomePage, QuickHomePage>();
                services.AddLazySingleton<IQuickDevicePage, QuickDevicePage>();
                services.AddLazySingleton<IQuickPerformancePage, QuickPerformancePage>();
                services.AddLazySingleton<IQuickProfilesPage, QuickProfilesPage>();
                services.AddLazySingleton<IQuickOverlayPage, QuickOverlayPage>();
                services.AddLazySingleton<IQuickSuspenderPage, QuickSuspenderPage>();

                //Sub Pages 
                services.AddLazySingleton<IJoysticksPage, JoysticksPage>();

                services.AddLogging();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConfiguration(context.Configuration);
                Log.Logger = new LoggerConfiguration()
                 .ReadFrom.Configuration(context.Configuration).CreateLogger();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddSerilog(dispose: true);

            })
            .Build();

    }

    /// <summary>
    ///     Invoked when the application is launched normally by the end user.  Other entry points
    ///     will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnStartup(StartupEventArgs args)
    {
        await _host.StartAsync();
        var settingsManager = _host.Services.GetService<SettingsManager>();
        //currentCulture = settingsManager.Value.GetString("CurrentCulture");

        LogManager.LogInformation("{0} ({1})", CurrentAssembly.GetName(), fileVersionInfo.FileVersion);

        using (var process = Process.GetCurrentProcess())
        {
            // force high priority
            SetPriorityClass(process.Handle, (int)PriorityClass.HIGH_PRIORITY_CLASS);

            var processes = Process.GetProcessesByName(process.ProcessName);
            if (processes.Length > 1)
                using (var prevProcess = processes[0])
                {
                    var handle = prevProcess.MainWindowHandle;
                    if (ProcessUtils.IsIconic(handle))
                        ProcessUtils.ShowWindow(handle, (int)ProcessUtils.ShowWindowCommands.Restored);

                    ProcessUtils.SetForegroundWindow(handle);

                    // force close this iteration
                    process.Kill();
                    return;
                }
        }

        switch (currentCulture)
        {
            default:
                cultureInfo = new CultureInfo("en-US");
                break;
            case "fr-FR":
            case "en-US":
            case "zh-CN":
            case "zh-Hant":
            case "de-DE":
            case "it-IT":
            case "pt-BR":
            case "es-ES":
            case "ja-JP":
            case "ru-RU":
                cultureInfo = new CultureInfo(currentCulture);
                break;
        }

        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        // handle exceptions nicely
        var currentDomain = default(AppDomain);
        currentDomain = AppDomain.CurrentDomain;
        // Handler for unhandled exceptions.
        currentDomain.UnhandledException += CurrentDomain_UnhandledException;
        // Handler for exceptions in threads behind forms.
        System.Windows.Forms.Application.ThreadException += Application_ThreadException;

        ServiceProvider = _host.Services;
        var mainWindow = _host.Services.GetService<MainWindow>();
        mainWindow?.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LogManager.LogCritical("OnExit");
    }
    private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        var ex = default(Exception);
        ex = (Exception)e.Exception;
        LogManager.LogCritical(ex.Message + "\t" + ex.StackTrace);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = default(Exception);
        ex = (Exception)e.ExceptionObject;
        LogManager.LogCritical(ex.Message + "\t" + ex.StackTrace);
    }
}