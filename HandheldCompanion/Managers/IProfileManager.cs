using System;
using System.IO;

namespace HandheldCompanion.Managers
{
    public interface IProfileManager
    {
        FileSystemWatcher profileWatcher { get; set; }

        event ProfileManager.AppliedEventHandler Applied;
        event ProfileManager.DeletedEventHandler Deleted;
        event ProfileManager.DiscardedEventHandler Discarded;
        event ProfileManager.InitializedEventHandler Initialized;
        event ProfileManager.UpdatedEventHandler Updated;

        bool Contains(Profile profile);
        bool Contains(string fileName);
        void CycleSubProfiles(bool previous = false);
        void DeleteProfile(Profile profile);
        void DeleteSubProfile(Profile subProfile);
        Profile GetCurrent();
        Profile GetDefault();
        Profile GetProfileForSubProfile(Profile subProfile);
        Profile GetProfileFromGuid(Guid Guid, bool ignoreStatus, bool isSubProfile = false);
        Profile GetProfileFromPath(string path, bool ignoreStatus);
        Profile? GetProfileWithDefaultLayout();
        Profile[] GetSubProfilesFromPath(string path, bool ignoreStatus);
        void SerializeProfile(Profile profile);
        void SetSubProfileAsFavorite(Profile subProfile);
        void Start();
        void Stop();
        void UpdateOrCreateProfile(Profile profile, UpdateSource source = UpdateSource.Background);
        bool UpdateProfileCloaking(Profile profile);
        bool UpdateProfileWrapper(Profile profile);
    }
}