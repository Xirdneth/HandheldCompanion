using System.Collections.Generic;

namespace HandheldCompanion.Managers
{
    public interface ISystemManager
    {
        bool IsInitialized { get; set; }

        event SystemManager.InitializedEventHandler Initialized;
        event SystemManager.PowerStatusChangedEventHandler PowerStatusChanged;
        event SystemManager.SystemStatusChangedEventHandler SystemStatusChanged;

        SortedDictionary<string, string> PowerStatusIcon { get; set; }
        void Start();
        void Stop();
    }
}