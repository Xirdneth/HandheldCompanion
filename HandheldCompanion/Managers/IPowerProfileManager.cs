using HandheldCompanion.Misc;
using System;
using System.Collections.Generic;

namespace HandheldCompanion.Managers
{
    public interface IPowerProfileManager
    {
        event PowerProfileManager.AppliedEventHandler Applied;
        event PowerProfileManager.DeletedEventHandler Deleted;
        event PowerProfileManager.DiscardedEventHandler Discarded;
        event PowerProfileManager.InitializedEventHandler Initialized;
        event PowerProfileManager.UpdatedEventHandler Updated;

        Dictionary<Guid, PowerProfile> profiles { get; set; }
        bool Contains(Guid guid);
        bool Contains(PowerProfile profile);
        void DeleteProfile(PowerProfile profile);
        PowerProfile GetCurrent();
        PowerProfile GetProfile(Guid guid);
        void SerializeProfile(PowerProfile profile);
        void Start();
        void Stop();
        void UpdateOrCreateProfile(PowerProfile profile, UpdateSource source);
    }
}