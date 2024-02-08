using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using HandheldCompanion.Views;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Invoked when the application is launched normally by the end user.  Other entry points
    ///     will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnStartup(StartupEventArgs args)
    {
        // get current assembly
        var CurrentAssembly = Assembly.GetExecutingAssembly();
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(CurrentAssembly.Location);

        // initialize log
        LogManager.Initialize("HandheldCompanion");
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

        // define culture settings
        var CurrentCulture = SettingsManager.GetString("CurrentCulture");
        var culture = CultureInfo.CurrentCulture;

        switch (CurrentCulture)
        {
            default:
                culture = new CultureInfo("en-US");
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
                culture = new CultureInfo(CurrentCulture);
                break;
        }

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // handle exceptions nicely
        var currentDomain = default(AppDomain);
        currentDomain = AppDomain.CurrentDomain;
        // Handler for unhandled exceptions.
        currentDomain.UnhandledException += CurrentDomain_UnhandledException;
        // Handler for exceptions in threads behind forms.
        System.Windows.Forms.Application.ThreadException += Application_ThreadException;

        try
        {
            string[] TestArgs = new string[]
            {
                "hello"
            };

            string[] pluginPaths = new string[]
            {
                // Paths to plugins to load.
                @"HandheldCompanion\HelloPlugin\bin\Debug\net8.0\HelloPlugin.dll"
            };

            IEnumerable<ICommand> commands = pluginPaths.SelectMany(pluginPath =>
            {
                Assembly pluginAssembly = LoadPlugin(pluginPath);
                return CreateCommands(pluginAssembly);
            }).ToList();

            if (TestArgs.Length == 0)
            {
                foreach (ICommand command in commands)
                {
                    LogManager.LogInformation($"{command.Name}\t - {command.Description}");
                }
            }
            else
            {
                foreach (string commandName in TestArgs)
                {
                    LogManager.LogInformation($"-- {commandName} --");

                    ICommand command = commands.FirstOrDefault(c => c.Name == commandName);
                    if (command == null)
                    {
                        LogManager.LogInformation("No such command is known.");
                        return;
                    }

                    command.Execute();

                    LogManager.LogInformation($"-- {commandName} --");
                }
            }
        }
        catch (Exception ex)
        {
            LogManager.LogError(ex.Message);
        }

        //MainWindow = new MainWindow(fileVersionInfo, CurrentAssembly);
        //MainWindow.Show();
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

    static Assembly LoadPlugin(string relativePath)
    {
        // Navigate up to the solution root
        string root = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(typeof(App).Assembly.Location)))))));

        string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        Console.WriteLine($"Loading commands from: {pluginLocation}");
        PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
    }

    static IEnumerable<ICommand> CreateCommands(Assembly assembly)
    {
        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(ICommand).IsAssignableFrom(type))
            {
                ICommand result = Activator.CreateInstance(type) as ICommand;
                if (result != null)
                {
                    count++;
                    yield return result;
                }
            }
        }

        if (count == 0)
        {
            string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
            throw new ApplicationException(
                $"Can't find any type which implements ICommand in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }
    }
}