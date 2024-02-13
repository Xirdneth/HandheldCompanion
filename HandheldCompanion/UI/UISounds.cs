using HandheldCompanion.Managers.Interfaces;
using iNKORE.UI.WPF.Modern.Controls;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace HandheldCompanion.UI
{
    public class UISounds : IUISounds
    {
        private string appFolder = string.Empty;
        private Timer soundTimer;
        private string audioFilePath;

        public string Expanded { get; } = "drop_001";
        public string Collapse { get; } = "drop_002";
        public const string Focus = "bong_001";
        public const string Click = "bong_001";
        public const string ToggleOn = "switch_004";
        public const string ToggleOff = "switch_005";
        public const string Select = "switch_007";
        public const string Slide = "glitch_004";
        private readonly Lazy<ISettingsManager> settingsManager;

        public UISounds(Lazy<ISettingsManager> settingsManager)
        {
            // Get the current application folder
            appFolder = AppDomain.CurrentDomain.BaseDirectory;

            soundTimer = new(100) { AutoReset = false };
            soundTimer.Elapsed += SoundTimer_Elapsed;

            // Register the class handler for the Click event
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnClick));
            EventManager.RegisterClassHandler(typeof(RepeatButton), RepeatButton.ClickEvent, new RoutedEventHandler(OnClick));
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.GotFocusEvent, new RoutedEventHandler(OnFocus));
            EventManager.RegisterClassHandler(typeof(ToggleSwitch), ToggleSwitch.ToggledEvent, new RoutedEventHandler(OnToggle));
            EventManager.RegisterClassHandler(typeof(CheckBox), CheckBox.CheckedEvent, new RoutedEventHandler(OnCheck));
            EventManager.RegisterClassHandler(typeof(CheckBox), CheckBox.UncheckedEvent, new RoutedEventHandler(OnCheck));
            EventManager.RegisterClassHandler(typeof(Slider), Slider.ValueChangedEvent, new RoutedEventHandler(OnSlide));
            EventManager.RegisterClassHandler(typeof(RadioButtons), RadioButtons.SelectionChangedEvent, new RoutedEventHandler(OnSelect));
            EventManager.RegisterClassHandler(typeof(Expander), Expander.ExpandedEvent, new RoutedEventHandler(OnExpand));
            EventManager.RegisterClassHandler(typeof(Expander), Expander.CollapsedEvent, new RoutedEventHandler(OnExpand));
            this.settingsManager = settingsManager;
        }

        private async void SoundTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            using (VorbisWaveReader waveReader = new VorbisWaveReader(audioFilePath))
            {
                using (WaveOutEvent waveOut = new WaveOutEvent())
                {
                    waveOut.Init(waveReader);
                    waveOut.Play();

                    // wait here until playback stops or should stop
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                        await Task.Delay(1);
                }
            }
        }

        private void OnExpand(object sender, RoutedEventArgs e)
        {
            Expander uIElement = (Expander)sender;
            if (!uIElement.IsVisible)
                return;

            switch (uIElement.IsExpanded)
            {
                case true:
                    PlayOggFile("drop_001");
                    break;
                case false:
                    PlayOggFile("drop_002");
                    break;
            }
        }

        private UIElement prevElement;
        private void OnFocus(object sender, RoutedEventArgs e)
        {
            UIElement uIElement = (UIElement)sender;
            if (!uIElement.IsFocused || !uIElement.Focusable || !uIElement.IsVisible)
                return;

            // set default sound
            string sound = UISounds.Focus;

            switch (uIElement.GetType().Name)
            {
                case "TouchScrollViewer":
                    return;
                case "ComboBoxItem":
                    if (prevElement != null && prevElement is ComboBox)
                    {
                        // ComboBox was opened
                        sound = this.Expanded;
                    }
                    break;
                case "ComboBox":
                    if (prevElement != null && prevElement is ComboBoxItem)
                    {
                        // ComboBox was closed
                        sound = this.Collapse;
                    }
                    break;
            }

            prevElement = uIElement;

            PlayOggFile(sound);
        }

        private void OnSelect(object sender, RoutedEventArgs e)
        {
            UIElement uIElement = (UIElement)sender;
            if (!uIElement.IsVisible)
                return;

            PlayOggFile(UISounds.Select);
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            UIElement uIElement = (UIElement)sender;
            if (!uIElement.IsVisible)
                return;

            PlayOggFile(UISounds.Focus);
        }

        private void OnCheck(object sender, RoutedEventArgs e)
        {
            CheckBox uIElement = (CheckBox)sender;
            if (!uIElement.IsLoaded || !uIElement.IsVisible)
                return;

            switch (uIElement.IsChecked)
            {
                case true:
                    PlayOggFile(UISounds.ToggleOn);
                    break;
                case false:
                    PlayOggFile(UISounds.ToggleOff);
                    break;
            }
        }

        private void OnToggle(object sender, RoutedEventArgs e)
        {
            ToggleSwitch uIElement = (ToggleSwitch)sender;
            if (!uIElement.IsLoaded || !uIElement.IsVisible)
                return;

            switch (uIElement.IsOn)
            {
                case true:
                    PlayOggFile(UISounds.ToggleOn);
                    break;
                case false:
                    PlayOggFile(UISounds.ToggleOff);
                    break;
            }
        }

        private void OnSlide(object sender, RoutedEventArgs e)
        {
            Control uIElement = (Control)sender;
            if (!uIElement.IsLoaded || !uIElement.IsVisible)
                return;

            PlayOggFile(UISounds.Slide);
        }

        public void PlayOggFile(string fileName)
        {
            bool Enabled = settingsManager.Value.GetBoolean("UISounds");
            if (!Enabled)
                return;

            // Concatenate /UI/Audio/{fileName}.ogg
            string audioFilePath = Path.Combine(appFolder, "UI", "Audio", fileName + ".ogg");
            if (!File.Exists(audioFilePath))
                return;

            // update file path
            this.audioFilePath = audioFilePath;

            // reset timer
            soundTimer.Stop();
            soundTimer.Start();
        }
    }
}
