using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using SharpDX.Direct3D9;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.QuickPages;

/// <summary>
///     Interaction logic for QuickHomePage.xaml
/// </summary>
public partial class QuickHomePage : Page
{
    private LockObject brightnessLock = new();
    private LockObject volumeLock = new();
    public Process CurrentProcess;

    public QuickHomePage(string Tag) : this()
    {
        this.Tag = Tag;

        HotkeysManager.HotkeyCreated += HotkeysManager_HotkeyCreated;
        HotkeysManager.HotkeyUpdated += HotkeysManager_HotkeyUpdated;

        MultimediaManager.VolumeNotification += SystemManager_VolumeNotification;
        MultimediaManager.BrightnessNotification += SystemManager_BrightnessNotification;
        MultimediaManager.Initialized += SystemManager_Initialized;


        ProcessManager.ForegroundChanged += ProcessManager_ForegroundChanged;


    }

    private void ProcessManager_ForegroundChanged(ProcessEx processEx, ProcessEx backgroundEx)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentProcess = processEx.Process;
            if (CurrentProcess != null)
            {
                string path = ProcessUtils.GetPathToApp(CurrentProcess.Id);

                string exec = Path.GetFileName(path);
                var ProcessId = CurrentProcess.Id;

                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                    if (icon is not null)
                    {
                        ProcessIcon.Source = icon.ToImageSource();
                    }
                }
                TitleTextBlock.Text = path;
                ExecutableTextBlock.Text = exec;
            }
        });
    }

    public QuickHomePage()
    {
        InitializeComponent();     
    }

    private void B_KillProcess_Clicked(object sender, RoutedEventArgs e)
    {
        if (CurrentProcess is not null)
            CurrentProcess.Kill();
    }

    private void HotkeysManager_HotkeyUpdated(Hotkey hotkey)
    {
        UpdatePins();
    }

    private void HotkeysManager_HotkeyCreated(Hotkey hotkey)
    {
        UpdatePins();
    }

    private void UpdatePins()
    {
        // todo, implement quick hotkey order
        QuickHotkeys.Children.Clear();

        foreach (var hotkey in HotkeysManager.Hotkeys.Values.Where(item => item.IsPinned))
            QuickHotkeys.Children.Add(hotkey.GetPin());
    }

    private void QuickButton_Click(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        MainWindow.overlayquickTools.NavView_Navigate(button.Name);
    }

    private void SystemManager_Initialized()
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (MultimediaManager.HasBrightnessSupport())
            {
                SliderBrightness.IsEnabled = true;
                SliderBrightness.Value = MultimediaManager.GetBrightness();
            }

            if (MultimediaManager.HasVolumeSupport())
            {
                SliderVolume.IsEnabled = true;
                SliderVolume.Value = MultimediaManager.GetVolume();
                UpdateVolumeIcon((float)SliderVolume.Value);
            }
        });
    }

    private void SystemManager_BrightnessNotification(int brightness)
    {
        // UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            using (new ScopedLock(brightnessLock))
                SliderBrightness.Value = brightness;
        });
    }

    private void SystemManager_VolumeNotification(float volume)
    {
        // UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            using (new ScopedLock(volumeLock))
            {
                UpdateVolumeIcon(volume);
                SliderVolume.Value = Math.Round(volume);
            }
        });
    }

    private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
            return;

        // wait until lock is released
        if (brightnessLock)
            return;

       MultimediaManager.SetBrightness(SliderBrightness.Value);
    }

    private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
            return;

        // wait until lock is released
        if (volumeLock)
            return;

        MultimediaManager.SetVolume(SliderVolume.Value);
    }

  

    private void UpdateVolumeIcon(float volume)
    {
        string glyph;

        if (volume == 0)
        {
            glyph = "\uE992"; // Mute icon
        }
        else if (volume <= 33)
        {
            glyph = "\uE993"; // Low volume icon
        }
        else if (volume <= 65)
        {
            glyph = "\uE994"; // Medium volume icon
        }
        else
        {
            glyph = "\uE995"; // High volume icon (default)
        }

        VolumeIcon.Glyph = glyph;
    }


}
