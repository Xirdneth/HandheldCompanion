using HandheldCompanion.Platforms;
using System.Diagnostics;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface IPlatformManager
    {
        event PlatformManager.InitializedEventHandler Initialized;

        Steam Steam { get; set; }
        GOGGalaxy GOGGalaxy { get; set; }
        UbisoftConnect UbisoftConnect { get; set; }
        RTSS RTSS { get; set; }
        Platforms.LibreHardwareMonitor libreHardwareMonitor { get; set; }
        PlatformType GetPlatform(Process proc);
        void Start();
        void Stop();
    }
}