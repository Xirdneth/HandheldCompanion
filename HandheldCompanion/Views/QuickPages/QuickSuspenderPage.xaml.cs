using HandheldCompanion.Controls;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Views.QuickPages.Interfaces;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HandheldCompanion.Views.QuickPages;

/// <summary>
///     Interaction logic for QuickSuspenderPage.xaml
/// </summary>
public partial class QuickSuspenderPage : Page, IQuickSuspenderPage
{
    private readonly Lazy<IProcessManager> processManager;


    public QuickSuspenderPage(Lazy<IProcessManager> processManager)
    {
        this.processManager = processManager;
        InitializeComponent();
    }

    public void SetTag(string Tag)
    {
        this.Tag = Tag;
    }

    public void Init()
    {
        processManager.Value.ProcessStarted += ProcessStarted;
        processManager.Value.ProcessStopped += ProcessStopped;

        // get processes
        foreach (ProcessEx processEx in processManager.Value.GetProcesses())
            ProcessStarted(processEx, true);
    }

    private void ProcessStopped(ProcessEx processEx)
    {
        try
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (CurrentProcesses.Children.Contains(processEx))
                    CurrentProcesses.Children.Remove(processEx);
            });
        }
        catch
        {
        }
    }

    private void ProcessStarted(ProcessEx processEx, bool OnStartup)
    {
        try
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (!CurrentProcesses.Children.Contains(processEx))
                    CurrentProcesses.Children.Add(processEx);
            });
        }
        catch
        {
            // process might have exited already
        }
    }
}