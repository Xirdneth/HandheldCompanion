using GameLib;
using HandheldCompanion.Extensions;
using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using HandheldCompanion.ViewModels;
using HandheldCompanion.Views;
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
    public App()
    {
        InitializeComponent();

        CurrentAssembly = Assembly.GetExecutingAssembly();
        fileVersionInfo = FileVersionInfo.GetVersionInfo(CurrentAssembly.Location);
        currentCulture = SettingsManager.GetString("CurrentCulture");
        cultureInfo = CultureInfo.CurrentCulture;

        _host = new HostBuilder()
            .ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                configurationBuilder.SetBasePath(context.HostingEnvironment.ContentRootPath);
                configurationBuilder.AddJsonFile("HandheldCompanion.json", optional: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLiteDb(@"HandheldCompanion.db");
                services.AddSingleton<MainWindow>();
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

        var mainWindow = _host.Services.GetService<MainWindow>();
        mainWindow?.Show();
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