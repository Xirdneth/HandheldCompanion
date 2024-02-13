using HandheldCompanion.Devices;
using HandheldCompanion.Managers.Desktop;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Misc;
using HandheldCompanion.Views.QuickPages.Interfaces;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.QuickPages;

/// <summary>
///     Interaction logic for QuickDevicePage.xaml
/// </summary>
public partial class QuickDevicePage : Page, IQuickDevicePage
{
    private IReadOnlyList<Radio> radios;
    private Timer radioTimer;
    private readonly Lazy<ISettingsManager> settingsManager;
    private readonly Lazy<IProfileManager> profileManager;
    private readonly Lazy<IMultimediaManager> multimediaManager;

    public QuickDevicePage(
        Lazy<ISettingsManager> settingsManager,
        Lazy<IProfileManager> profileManager,
        Lazy<IMultimediaManager> multimediaManager)
    {
        this.settingsManager = settingsManager;
        this.profileManager = profileManager;
        this.multimediaManager = multimediaManager;
        InitializeComponent();
    }

    public void SetTag(string Tag)
    {
        this.Tag = Tag;
    }

    public void Init()
    {
        multimediaManager.Value.PrimaryScreenChanged += DesktopManager_PrimaryScreenChanged;
        multimediaManager.Value.DisplaySettingsChanged += DesktopManager_DisplaySettingsChanged;
        settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;
        profileManager.Value.Applied += ProfileManager_Applied;
        profileManager.Value.Discarded += ProfileManager_Discarded;

        LegionGoPanel.Visibility = MainWindow.CurrentDevice is LegionGo ? Visibility.Visible : Visibility.Collapsed;
        DynamicLightingPanel.IsEnabled = MainWindow.CurrentDevice.Capabilities.HasFlag(DeviceCapabilities.DynamicLighting);

        NightLightToggle.IsEnabled = NightLight.Supported;
        NightLightToggle.IsOn = NightLight.Enabled;

        // manage events
        NightLight.Toggled += NightLight_Toggled;

        radioTimer = new(1000);
        radioTimer.Elapsed += RadioTimer_Elapsed;
        radioTimer.Start();
    }

    private void ProfileManager_Applied(Profile profile, UpdateSource source)
    {
        // Go to profile integer scaling resolution
        if (profile.IntegerScalingEnabled)
        {
            DesktopScreen desktopScreen = multimediaManager.Value.GetDesktopScreen();
            var profileResolution = desktopScreen?.screenDividers.FirstOrDefault(d => d.divider == profile.IntegerScalingDivider);
            if (profileResolution is not null)
            {
                SetResolution(profileResolution.resolution);
            }
        }
        else
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // Revert back to resolution in device settings
                SetResolution();
            });
        }

        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var canChangeDisplay = !profile.IntegerScalingEnabled;
            DisplayStack.IsEnabled = canChangeDisplay;
            ResolutionOverrideStack.Visibility = canChangeDisplay ? Visibility.Collapsed : Visibility.Visible;

        });
    }

    private void ProfileManager_Discarded(Profile profile)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            SetResolution();

            if (profile.IntegerScalingEnabled)
            {
                DisplayStack.IsEnabled = true;
                ResolutionOverrideStack.Visibility = Visibility.Collapsed;
            }
        });
    }

    private void NightLight_Toggled(bool enabled)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            NightLightToggle.IsOn = enabled;
        });
    }

    private void SettingsManager_SettingValueChanged(string? name, object value)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            switch (name)
            {
                case "LEDSettingsEnabled":
                    UseDynamicLightingToggle.IsOn = Convert.ToBoolean(value);
                    break;
            }
        });
    }

    private void RadioTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        new Task(async () =>
        {
            // Get the Bluetooth adapter
            BluetoothAdapter adapter = await BluetoothAdapter.GetDefaultAsync();

            // Get the Bluetooth radio
            radios = await Radio.GetRadiosAsync();

            // UI thread (async)
            _ = Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // WIFI
                WifiToggle.IsEnabled = radios.Where(radio => radio.Kind == RadioKind.WiFi).Any();
                WifiToggle.IsOn = radios.Where(radio => radio.Kind == RadioKind.WiFi && radio.State == RadioState.On).Any();

                // Bluetooth
                BluetoothToggle.IsEnabled = radios.Where(radio => radio.Kind == RadioKind.Bluetooth).Any();
                BluetoothToggle.IsOn = radios.Where(radio => radio.Kind == RadioKind.Bluetooth && radio.State == RadioState.On).Any();
            });
        }).Start();
    }

    private void DesktopManager_PrimaryScreenChanged(DesktopScreen screen)
    {
        ComboBoxResolution.Items.Clear();
        foreach (ScreenResolution resolution in screen.screenResolutions)
            ComboBoxResolution.Items.Add(resolution);
    }

    private void DesktopManager_DisplaySettingsChanged(ScreenResolution resolution)
    {
        // We don't want to change the combobox when it's changed from profile integer scaling
        var currentProfile = profileManager.Value.GetCurrent();
        if (ComboBoxResolution.SelectedItem is not null && currentProfile is not null && currentProfile.IntegerScalingEnabled)
            return;

        ComboBoxResolution.SelectedItem = resolution;

        int screenFrequency = multimediaManager.Value.GetDesktopScreen().GetCurrentFrequency();
        foreach (ComboBoxItem comboBoxItem in ComboBoxFrequency.Items)
        {
            if (comboBoxItem.Tag is int frequency)
            {
                if (frequency == screenFrequency)
                {
                    ComboBoxFrequency.SelectedItem = comboBoxItem;
                    break;
                }
            }
        }
    }

    private void ComboBoxResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboBoxResolution.SelectedItem is null)
            return;

        ScreenResolution resolution = (ScreenResolution)ComboBoxResolution.SelectedItem;
        int screenFrequency = multimediaManager.Value.GetDesktopScreen().GetCurrentFrequency();

        ComboBoxFrequency.Items.Clear();
        foreach (int frequency in resolution.Frequencies.Keys)
        {
            ComboBoxItem comboBoxItem = new()
            {
                Content = $"{frequency} Hz",
                Tag = frequency,
            };

            ComboBoxFrequency.Items.Add(comboBoxItem);

            if (frequency == screenFrequency)
                ComboBoxFrequency.SelectedItem = comboBoxItem;
        }

        SetResolution();
    }

    private void ComboBoxFrequency_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboBoxFrequency.SelectedItem is null)
            return;

        SetResolution();
    }

    private void SetResolution()
    {
        if (ComboBoxResolution.SelectedItem is null)
            return;

        if (ComboBoxFrequency.SelectedItem is null)
            return;

        ScreenResolution resolution = (ScreenResolution)ComboBoxResolution.SelectedItem;
        int frequency = (int)((ComboBoxItem)ComboBoxFrequency.SelectedItem).Tag;

        // update current screen resolution
        DesktopScreen desktopScreen = multimediaManager.Value.GetDesktopScreen();

        if (desktopScreen.devMode.dmPelsWidth == resolution.Width &&
            desktopScreen.devMode.dmPelsHeight == resolution.Height &&
            desktopScreen.devMode.dmDisplayFrequency == frequency &&
            desktopScreen.devMode.dmBitsPerPel == resolution.BitsPerPel)
            return;

        multimediaManager.Value.SetResolution(resolution.Width, resolution.Height, frequency, resolution.BitsPerPel);
    }

    public void SetResolution(ScreenResolution resolution)
    {
        // update current screen resolution
        multimediaManager.Value.SetResolution(resolution.Width, resolution.Height, multimediaManager.Value.GetDesktopScreen().GetCurrentFrequency(), resolution.BitsPerPel);
    }

    private void WIFIToggle_Toggled(object sender, RoutedEventArgs e)
    {
        foreach (Radio radio in radios.Where(r => r.Kind == RadioKind.WiFi))
            _ = radio.SetStateAsync(WifiToggle.IsOn ? RadioState.On : RadioState.Off);
    }

    private void BluetoothToggle_Toggled(object sender, RoutedEventArgs e)
    {
        foreach (Radio radio in radios.Where(r => r.Kind == RadioKind.Bluetooth))
            _ = radio.SetStateAsync(BluetoothToggle.IsOn ? RadioState.On : RadioState.Off);
    }

    private void UseDynamicLightingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("LEDSettingsEnabled", UseDynamicLightingToggle.IsOn);
    }

    public void Close()
    {
        radioTimer.Stop();
    }

    private void Toggle_cFFanSpeed_Toggled(object sender, RoutedEventArgs e)
    {
        if (MainWindow.CurrentDevice is ILegionGo device)
        {
            ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
            device.SetFanFullSpeedAsync(toggleSwitch.IsOn);
        }
    }

    private void NightLightToggle_Toggled(object sender, RoutedEventArgs e)
    {
        NightLight.Enabled = NightLightToggle.IsOn;
    }
}