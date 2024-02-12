using HandheldCompanion.Managers.Desktop;
using System.Collections.Generic;

namespace HandheldCompanion.Managers
{
    public interface IMultimediaManager
    {
        event MultimediaManager.BrightnessNotificationEventHandler BrightnessNotification;
        event MultimediaManager.DisplayOrientationChangedEventHandler DisplayOrientationChanged;
        event MultimediaManager.DisplaySettingsChangedEventHandler DisplaySettingsChanged;
        event MultimediaManager.InitializedEventHandler Initialized;
        event MultimediaManager.PrimaryScreenChangedEventHandler PrimaryScreenChanged;
        event MultimediaManager.VolumeNotificationEventHandler VolumeNotification;

        short GetBrightness();
        DesktopScreen GetDesktopScreen();
        MultimediaManager.Display GetDisplay(string DeviceName);
        List<MultimediaManager.Display> GetResolutions(string DeviceName);
        ScreenRotation GetScreenOrientation();
        double GetVolume();
        bool HasBrightnessSupport();
        bool HasVolumeSupport();
        void PlayWindowsMedia(string file);
        void SetBrightness(double brightness);
        void SetDefaultAudioEndPoint();
        bool SetResolution(int width, int height, int displayFrequency);
        bool SetResolution(int width, int height, int displayFrequency, int bitsPerPel);
        void SetVolume(double volume);
        void Start();
        void Stop();
    }
}