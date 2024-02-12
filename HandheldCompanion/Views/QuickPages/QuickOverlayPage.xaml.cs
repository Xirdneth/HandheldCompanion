using HandheldCompanion.Managers;
using HandheldCompanion.Platforms;
using System;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.QuickPages;

/// <summary>
///     Interaction logic for QuickOverlayPage.xaml
/// </summary>
public partial class QuickOverlayPage : Page
{
    private readonly Lazy<ISettingsManager> settingsManager;
    private readonly Lazy<IPlatformManager> platformManager;

    public QuickOverlayPage(string Tag,
        Lazy<ISettingsManager> settingsManager,
        Lazy<IPlatformManager> platformManager) : this(settingsManager, platformManager)
    {
        this.Tag = Tag;
    }

    public QuickOverlayPage(
        Lazy<ISettingsManager> settingsManager, 
        Lazy<IPlatformManager> platformManager)
    {
        InitializeComponent();
        this.settingsManager = settingsManager;
        this.platformManager = platformManager;

        settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;

        platformManager.Value.RTSS.Updated += RTSS_Updated;
        platformManager.Value.libreHardwareMonitor.Updated += LibreHardwareMonitor_Updated;

        // force call
        // todo: make PlatformManager static
        RTSS_Updated(platformManager.Value.RTSS.Status);
        LibreHardwareMonitor_Updated(platformManager.Value.libreHardwareMonitor.Status);

    }

    private void LibreHardwareMonitor_Updated(PlatformStatus status)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            switch (status)
            {
                case PlatformStatus.Ready:
                    OverlayDisplayLevelExtended.IsEnabled = true;
                    OverlayDisplayLevelFull.IsEnabled = true;
                    break;
                case PlatformStatus.Stalled:
                    // OverlayDisplayLevelExtended.IsEnabled = false;
                    // OverlayDisplayLevelFull.IsEnabled = false;
                    break;
            }
        });
    }

    private void RTSS_Updated(PlatformStatus status)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            switch (status)
            {
                case PlatformStatus.Ready:
                    ComboBoxOverlayDisplayLevel.IsEnabled = true;
                    break;
                case PlatformStatus.Stalled:
                    ComboBoxOverlayDisplayLevel.SelectedIndex = 0;
                    break;
            }
        });
    }

    private void SettingsManager_SettingValueChanged(string name, object value)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            switch (name)
            {
                case "OnScreenDisplayLevel":
                    var index = Convert.ToInt32(value);
                    ComboBoxOverlayDisplayLevel.SelectedIndex = index;
                    StackCustomSettings.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case "OnScreenDisplayTimeLevel":
                    ComboBoxOnScreenDisplayTimeLevel.SelectedIndex = Convert.ToInt32(value);
                    break;
                case "OnScreenDisplayFPSLevel":
                    ComboBoxOnScreenDisplayFPSLevel.SelectedIndex = Convert.ToInt32(value);
                    break;
                case "OnScreenDisplayCPULevel":
                    ComboBoxOnScreenDisplayCPULevel.SelectedIndex = Convert.ToInt32(value);
                    break;
                case "OnScreenDisplayGPULevel":
                    ComboBoxOnScreenDisplayGPULevel.SelectedIndex = Convert.ToInt32(value);
                    break;
                case "OnScreenDisplayRAMLevel":
                    ComboBoxOnScreenDisplayRAMLevel.SelectedIndex = Convert.ToInt32(value);
                    break;
                case "OnScreenDisplayVRAMLevel":
                    ComboBoxOnScreenDisplayVRAMLevel.SelectedIndex = Convert.ToInt32(value);
                    break;
                case "OnScreenDisplayBATTLevel":
                    ComboBoxOnScreenDisplayBATTLevel.SelectedIndex = Convert.ToInt32(value);
                    break;
            }
        });
    }

    private void ComboBoxOverlayDisplayLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("OnScreenDisplayLevel", ComboBoxOverlayDisplayLevel.SelectedIndex);
    }

    private void ComboBoxOnScreenDisplayTimeLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("OnScreenDisplayTimeLevel", ComboBoxOnScreenDisplayTimeLevel.SelectedIndex);
    }

    private void ComboBoxOnScreenDisplayFPSLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("OnScreenDisplayFPSLevel", ComboBoxOnScreenDisplayFPSLevel.SelectedIndex);
    }

    private void ComboBoxOnScreenDisplayCPULevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("OnScreenDisplayCPULevel", ComboBoxOnScreenDisplayCPULevel.SelectedIndex);
    }

    private void ComboBoxOnScreenDisplayRAMLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("OnScreenDisplayRAMLevel", ComboBoxOnScreenDisplayRAMLevel.SelectedIndex);
    }

    private void ComboBoxOnScreenDisplayGPULevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("OnScreenDisplayGPULevel", ComboBoxOnScreenDisplayGPULevel.SelectedIndex);
    }

    private void ComboBoxOnScreenDisplayVRAMLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("OnScreenDisplayVRAMLevel", ComboBoxOnScreenDisplayVRAMLevel.SelectedIndex);
    }

    private void ComboBoxOnScreenDisplayBATTLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("OnScreenDisplayBATTLevel", ComboBoxOnScreenDisplayBATTLevel.SelectedIndex);
    }
}