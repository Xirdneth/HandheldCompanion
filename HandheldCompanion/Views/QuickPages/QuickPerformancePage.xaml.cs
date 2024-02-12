using HandheldCompanion.Devices;
using HandheldCompanion.Managers;
using HandheldCompanion.Managers.Desktop;
using HandheldCompanion.Misc;
using HandheldCompanion.Platforms;
using HandheldCompanion.Processors;
using HandheldCompanion.Utils;
using HandheldCompanion.Views.Windows;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.QuickPages;

/// <summary>
///     Interaction logic for QuickPerformancePage.xaml
/// </summary>
public partial class QuickPerformancePage : Page
{
    private const int UpdateInterval = 500;
    private readonly Timer UpdateTimer;
    private readonly Lazy<ISettingsManager> settingsManager;
    private readonly Lazy<IPlatformManager> platformManager;
    private readonly Lazy<IPerformanceManager> performanceManager;
    private readonly Lazy<IPowerProfileManager> powerProfileManager;
    private readonly Lazy<IMultimediaManager> multimediaManager;
    private PowerProfile selectedProfile;

    private LockObject updateLock = new();

    public QuickPerformancePage(string Tag,
        Lazy<ISettingsManager> settingsManager,
        Lazy<IPlatformManager> platformManager,
        Lazy<IPerformanceManager> performanceManager,
        Lazy<IPowerProfileManager> powerProfileManager,
        Lazy<IMultimediaManager> multimediaManager) : this(settingsManager, platformManager, performanceManager, powerProfileManager, multimediaManager)
    {
        this.Tag = Tag;
    }

    public QuickPerformancePage(
        Lazy<ISettingsManager> settingsManager,
        Lazy<IPlatformManager> platformManager,
        Lazy<IPerformanceManager> performanceManager,
        Lazy<IPowerProfileManager> powerProfileManager,
        Lazy<IMultimediaManager> multimediaManager)
    {
        InitializeComponent();

        this.settingsManager = settingsManager;
        this.platformManager = platformManager;
        this.performanceManager = performanceManager;
        this.powerProfileManager = powerProfileManager;
        this.multimediaManager = multimediaManager;
        /*
performanceManager.Value.PowerModeChanged += PerformanceManager_PowerModeChanged;
performanceManager.Value.PerfBoostModeChanged += PerformanceManager_PerfBoostModeChanged;
performanceManager.Value.EPPChanged += PerformanceManager_EPPChanged;
*/

        // manage events
        settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;
        platformManager.Value.RTSS.Updated += RTSS_Updated;
        performanceManager.Value.ProcessorStatusChanged += PerformanceManager_StatusChanged;
        performanceManager.Value.EPPChanged += PerformanceManager_EPPChanged;
        performanceManager.Value.Initialized += PerformanceManager_Initialized;
        powerProfileManager.Value.Updated += PowerProfileManager_Updated;
        powerProfileManager.Value.Deleted += PowerProfileManager_Deleted;
        multimediaManager.Value.PrimaryScreenChanged += SystemManager_PrimaryScreenChanged;

        // device settings
        GPUSlider.Minimum = MainWindow.CurrentDevice.GfxClock[0];
        GPUSlider.Maximum = MainWindow.CurrentDevice.GfxClock[1];

        CPUSlider.Minimum = MotherboardInfo.ProcessorMaxTurboSpeed / 4.0d;
        CPUSlider.Maximum = MotherboardInfo.ProcessorMaxTurboSpeed;

        // motherboard settings
        CPUCoreSlider.Maximum = MotherboardInfo.NumberOfCores;

        FanModeSoftware.IsEnabled = MainWindow.CurrentDevice.Capabilities.HasFlag(DeviceCapabilities.FanControl);

        UpdateTimer = new Timer(UpdateInterval);
        UpdateTimer.AutoReset = false;
        UpdateTimer.Elapsed += (sender, e) => SubmitProfile();

        // force call
        RTSS_Updated(platformManager.Value.RTSS.Status);
        
    }

    private void SystemManager_PrimaryScreenChanged(DesktopScreen desktopScreen)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            AutoTDPSlider.Maximum = desktopScreen.devMode.dmDisplayFrequency;
        });
    }

    private void PowerProfileManager_Deleted(PowerProfile profile)
    {
        // current power profile deleted, return to previous page
        bool isCurrent = selectedProfile?.Guid == profile.Guid;
        if (isCurrent)
            MainWindow.overlayquickTools.ContentFrame.GoBack();
    }

    private void PowerProfileManager_Updated(PowerProfile profile, UpdateSource source)
    {
        if (selectedProfile is null)
            return;

        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // current power profile updated, update UI
            bool isCurrent = selectedProfile.Guid == profile.Guid;
            if (isCurrent)
                UpdateUI();
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
                    var Processor = performanceManager.Value.GetProcessor();
                    StackProfileAutoTDP.IsEnabled = true && Processor is not null ? Processor.CanChangeTDP : false;
                    break;
                case PlatformStatus.Stalled:
                    // StackProfileFramerate.IsEnabled = false;
                    // StackProfileAutoTDP.IsEnabled = false;
                    break;
            }
        });
    }

    public void UpdateProfile()
    {
        if (UpdateTimer is not null)
        {
            UpdateTimer.Stop();
            UpdateTimer.Start();
        }
    }

    public void SubmitProfile(UpdateSource source = UpdateSource.ProfilesPage)
    {
        if (selectedProfile is null)
            return;

        powerProfileManager.Value.UpdateOrCreateProfile(selectedProfile, source);
    }

    private void PerformanceManager_StatusChanged(bool CanChangeTDP, bool CanChangeGPU)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            StackProfileTDP.IsEnabled = CanChangeTDP;
            StackProfileAutoTDP.IsEnabled = CanChangeTDP && platformManager.Value.RTSS.IsInstalled;

            StackProfileGPUClock.IsEnabled = CanChangeGPU;
        });
    }

    private void PerformanceManager_EPPChanged(uint EPP)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            EPPSlider.Value = EPP;
        });
    }

    private void PerformanceManager_Initialized()
    {
        Processor processor = performanceManager.Value.GetProcessor();
        if (processor is null)
            return;

        PerformanceManager_StatusChanged(processor.CanChangeTDP, processor.CanChangeGPU);
    }

    /*
    private void PerformanceManager_PowerModeChanged(int idx)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() => { PowerModeSlider.Value = idx; });
    }

    private void PerformanceManager_PerfBoostModeChanged(bool value)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() => { CPUBoostToggle.IsOn = value; });
    }
    */

    private void SettingsManager_SettingValueChanged(string name, object value)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            switch (name)
            {
                case "ConfigurableTDPOverrideDown":
                    {
                        using (new ScopedLock(updateLock))
                        {
                            TDPSlider.Minimum = (double)value;
                        }
                    }
                    break;
                case "ConfigurableTDPOverrideUp":
                    {
                        using (new ScopedLock(updateLock))
                        {
                            TDPSlider.Maximum = (double)value;
                        }
                    }
                    break;
            }
        });
    }

    private void PowerMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PowerMode.SelectedIndex == -1)
            return;

        if (selectedProfile is null)
            return;

        // wait until lock is released
        if (updateLock)
            return;

        selectedProfile.OSPowerMode = performanceManager.Value.PowerModes[PowerMode.SelectedIndex];
        UpdateProfile();
    }

    private void CPUBoostLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CPUBoostLevel.SelectedIndex == -1)
            return;

        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.CPUBoostLevel = CPUBoostLevel.SelectedIndex;
        UpdateProfile();
    }

    public void SelectionChanged(Guid guid)
    {
        // if an update is pending, cut it short, it will disturb profile selection though
        // keep me ?
        if (UpdateTimer.Enabled)
        {
            UpdateTimer.Stop();
            SubmitProfile();
        }

        selectedProfile = powerProfileManager.Value.GetProfile(guid);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (selectedProfile is null)
            return;

        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            using (new ScopedLock(updateLock))
            {
                // we shouldn't allow users to modify some of default profile settings
                PowerSettingsPanel.IsEnabled = !selectedProfile.DeviceDefault;
                Button_PowerSettings_Delete.IsEnabled = !selectedProfile.Default;

                // page name
                this.Title = selectedProfile.Name;

                // TDP
                TDPToggle.IsOn = selectedProfile.TDPOverrideEnabled;
                var TDP = selectedProfile.TDPOverrideValues is not null
                    ? selectedProfile.TDPOverrideValues
                    : MainWindow.CurrentDevice.nTDP;
                TDPSlider.Value = TDP[(int)PowerType.Slow];

                // CPU Clock control
                CPUToggle.IsOn = selectedProfile.CPUOverrideEnabled;
                CPUSlider.Value = selectedProfile.CPUOverrideValue != 0 ? selectedProfile.CPUOverrideValue : MotherboardInfo.ProcessorMaxTurboSpeed;

                // GPU Clock control
                GPUToggle.IsOn = selectedProfile.GPUOverrideEnabled;
                GPUSlider.Value = selectedProfile.GPUOverrideValue != 0 ? selectedProfile.GPUOverrideValue : 255 * 50;

                // AutoTDP
                AutoTDPToggle.IsOn = selectedProfile.AutoTDPEnabled;
                AutoTDPSlider.Value = selectedProfile.AutoTDPRequestedFPS;

                // EPP
                EPPToggle.IsOn = selectedProfile.EPPOverrideEnabled;
                EPPSlider.Value = selectedProfile.EPPOverrideValue;

                // CPU Core Count
                CPUCoreToggle.IsOn = selectedProfile.CPUCoreEnabled;
                CPUCoreSlider.Value = selectedProfile.CPUCoreCount;

                // CPU Boost
                CPUBoostLevel.SelectedIndex = selectedProfile.CPUBoostLevel;

                // Power Mode
                PowerMode.SelectedIndex = Array.IndexOf(performanceManager.Value.PowerModes, selectedProfile.OSPowerMode);

                // Fan control
                FanMode.SelectedIndex = (int)selectedProfile.FanProfile.fanMode;
            }
        });
    }

    private void TDPToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.TDPOverrideEnabled = TDPToggle.IsOn;
        selectedProfile.TDPOverrideValues = new double[3]
        {
                TDPSlider.Value,
                TDPSlider.Value,
                TDPSlider.Value
        };

        UpdateProfile();
    }

    private void TDPSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.TDPOverrideValues = new double[3]
        {
                TDPSlider.Value,
                TDPSlider.Value,
                TDPSlider.Value
        };

        UpdateProfile();
    }

    private void AutoTDPToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.AutoTDPEnabled = AutoTDPToggle.IsOn;
        AutoTDPSlider.Value = selectedProfile.AutoTDPRequestedFPS;

        UpdateProfile();
    }

    private void AutoTDPRequestedFPSSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.AutoTDPRequestedFPS = (int)AutoTDPSlider.Value;
        UpdateProfile();
    }

    private void CPUToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.CPUOverrideEnabled = CPUToggle.IsOn;
        selectedProfile.CPUOverrideValue = (int)CPUSlider.Value;
        UpdateProfile();
    }

    private void CPUSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.CPUOverrideValue = (int)CPUSlider.Value;
        UpdateProfile();
    }

    private void GPUToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.GPUOverrideEnabled = GPUToggle.IsOn;
        selectedProfile.GPUOverrideValue = (int)GPUSlider.Value;
        UpdateProfile();
    }

    private void GPUSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.GPUOverrideValue = (int)GPUSlider.Value;
        UpdateProfile();
    }

    private void EPPToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.EPPOverrideEnabled = EPPToggle.IsOn;
        selectedProfile.EPPOverrideValue = (uint)EPPSlider.Value;
        UpdateProfile();
    }

    private void EPPSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (selectedProfile is null)
            return;

        if (updateLock)
            return;

        selectedProfile.EPPOverrideValue = (uint)EPPSlider.Value;
        UpdateProfile();
    }

    private void CPUCoreToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (selectedProfile is null)
            return;

        // wait until lock is released
        if (updateLock)
            return;

        selectedProfile.CPUCoreEnabled = CPUCoreToggle.IsOn;
        selectedProfile.CPUCoreCount = (int)CPUCoreSlider.Value;
        UpdateProfile();
    }

    private void CPUCoreSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (selectedProfile is null)
            return;

        if (!CPUCoreSlider.IsInitialized)
            return;

        // wait until lock is released
        if (updateLock)
            return;

        selectedProfile.CPUCoreCount = (int)CPUCoreSlider.Value;
        UpdateProfile();
    }

    private void FanMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FanMode.SelectedIndex == -1)
            return;

        if (selectedProfile is null)
            return;

        // wait until lock is released
        if (updateLock)
            return;

        selectedProfile.FanProfile.fanMode = (FanMode)FanMode.SelectedIndex;
        UpdateProfile();
    }

    private async void Button_PowerSettings_Delete_Click(object sender, RoutedEventArgs e)
    {
        var result = Dialog.ShowAsync(
                $"{Properties.Resources.ProfilesPage_AreYouSureDelete1} \"{selectedProfile.Name}\"?",
                $"{Properties.Resources.ProfilesPage_AreYouSureDelete2}",
                ContentDialogButton.Primary,
                $"{Properties.Resources.ProfilesPage_Cancel}",
                $"{Properties.Resources.ProfilesPage_Delete}", string.Empty, OverlayQuickTools.GetCurrent());
        await result; // sync call

        switch (result.Result)
        {
            case ContentDialogResult.Primary:
                powerProfileManager.Value.DeleteProfile(selectedProfile);
                break;
        }
    }
}