using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using System;

namespace HandheldCompanion.Devices;

public class DefaultDevice : IDevice
{
    public DefaultDevice(
        Lazy<ISettingsManager> settingsManager,
        Lazy<IPowerProfileManager> powerProfileManager,
        Lazy<IControllerManager> controllerManager,
        Lazy<ISystemManager> systemManager,
        Lazy<ITimerManager> timerManager) : base(settingsManager, powerProfileManager, controllerManager, systemManager, timerManager)
    {
        // We assume all the devices have those keys
        // Disabled until we implement "turbo" type hotkeys

        /*
        OEMChords.Add(new DeviceChord("Volume Up",
            new List<KeyCode> { KeyCode.VolumeUp },
            new List<KeyCode> { KeyCode.VolumeUp },
            false, ButtonFlags.VolumeUp
        ));
        OEMChords.Add(new DeviceChord("Volume Down",
            new List<KeyCode> { KeyCode.VolumeDown },
            new List<KeyCode> { KeyCode.VolumeDown },
            false, ButtonFlags.VolumeDown
        ));
        */
    }
}