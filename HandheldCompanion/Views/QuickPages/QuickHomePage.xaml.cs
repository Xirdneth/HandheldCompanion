using HandheldCompanion.Managers;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Utils;
using HandheldCompanion.Views.QuickPages.Interfaces;
using HandheldCompanion.Views.Windows.Interfaces;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.QuickPages;

/// <summary>
///     Interaction logic for QuickHomePage.xaml
/// </summary>
public partial class QuickHomePage : Page, IQuickHomePage
{
    private LockObject brightnessLock = new();
    private LockObject volumeLock = new();
    private readonly Lazy<ISettingsManager> settingsManager;
    private readonly Lazy<IHotkeysManager> hotkeysManager;
    private readonly Lazy<IMultimediaManager> multimediaManager;
    private readonly Lazy<IProfileManager> profileManager;
    private readonly Lazy<IOverlayQuickTools> overlayQuickTools;

    public QuickHomePage(
        Lazy<ISettingsManager> settingsManager,
        Lazy<IHotkeysManager> hotkeysManager,
        Lazy<IMultimediaManager> multimediaManager,
        Lazy<IProfileManager> profileManager,
        Lazy<IOverlayQuickTools> overlayQuickTools)
    {
        this.settingsManager = settingsManager;
        this.hotkeysManager = hotkeysManager;
        this.multimediaManager = multimediaManager;
        this.profileManager = profileManager;
        this.overlayQuickTools = overlayQuickTools;
        InitializeComponent();
    }

    public void SetTag(string Tag)
    {
        this.Tag = Tag;
    }

    public void Init()
    {
        hotkeysManager.Value.HotkeyCreated += HotkeysManager_HotkeyCreated;
        hotkeysManager.Value.HotkeyUpdated += HotkeysManager_HotkeyUpdated;

        multimediaManager.Value.VolumeNotification += SystemManager_VolumeNotification;
        multimediaManager.Value.BrightnessNotification += SystemManager_BrightnessNotification;
        multimediaManager.Value.Initialized += SystemManager_Initialized;

        profileManager.Value.Applied += ProfileManager_Applied;
        settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;
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

        foreach (var hotkey in hotkeysManager.Value.Hotkeys.Values.Where(item => item.IsPinned))
            QuickHotkeys.Children.Add(hotkey.GetPin());
    }

    private void QuickButton_Click(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        overlayQuickTools.Value.NavView_Navigate(button.Name);
    }

    private void SystemManager_Initialized()
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (multimediaManager.Value.HasBrightnessSupport())
            {
                SliderBrightness.IsEnabled = true;
                SliderBrightness.Value = multimediaManager.Value.GetBrightness();
            }

            if (multimediaManager.Value.HasVolumeSupport())
            {
                SliderVolume.IsEnabled = true;
                SliderVolume.Value = multimediaManager.Value.GetVolume();
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

        multimediaManager.Value.SetBrightness(SliderBrightness.Value);
    }

    private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
            return;

        // wait until lock is released
        if (volumeLock)
            return;

        multimediaManager.Value.SetVolume(SliderVolume.Value);
    }

    private void ProfileManager_Applied(Profile profile, UpdateSource source)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            t_CurrentProfile.Text = profile.ToString();
        });
    }

    private void SettingsManager_SettingValueChanged(string name, object value)
    {
        string[] onScreenDisplayLevels = {
            Properties.Resources.OverlayPage_OverlayDisplayLevel_Disabled,
            Properties.Resources.OverlayPage_OverlayDisplayLevel_Minimal,
            Properties.Resources.OverlayPage_OverlayDisplayLevel_Extended,
            Properties.Resources.OverlayPage_OverlayDisplayLevel_Full,
            Properties.Resources.OverlayPage_OverlayDisplayLevel_Custom,
            Properties.Resources.OverlayPage_OverlayDisplayLevel_External,
        };

        switch (name)
        {
            case "OnScreenDisplayLevel":
                {
                    var overlayLevel = Convert.ToInt16(value);

                    // UI thread (async)
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        t_CurrentOverlayLevel.Text = onScreenDisplayLevels[overlayLevel];
                    });
                }
                break;
        }
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
