using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using HandheldCompanion.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace HandheldCompanion.Managers
{
    public class PowerProfileManager : IPowerProfileManager
    {
        private PowerProfile currentProfile;

        public Dictionary<Guid, PowerProfile> profiles { get; set; } = default;

        private string ProfilesPath;

        public bool IsInitialized;
        private readonly Lazy<IProfileManager> profileManager;
        private readonly IPowerProfile powerProfile;
        private readonly Lazy<IPlatformManager> platformManager;
        private readonly Lazy<IToastManager> toastManager;

        public PowerProfileManager(
            Lazy<IProfileManager> profileManager, 
            IPowerProfile powerProfile, 
            Lazy<IPlatformManager> platformManager,
            Lazy<IToastManager> toastManager)
        {
            this.profileManager = profileManager;
            this.powerProfile = powerProfile;
            this.platformManager = platformManager;
            this.toastManager = toastManager;
            // initialiaze path(s)
            ProfilesPath = Path.Combine(MainWindow.SettingsPath, "powerprofiles");
            if (!Directory.Exists(ProfilesPath))
                Directory.CreateDirectory(ProfilesPath);
            profiles = new();

        }

        public void Start()
        {
            platformManager.Value.libreHardwareMonitor.CPUTemperatureChanged += LibreHardwareMonitor_CpuTemperatureChanged;

            this.profileManager.Value.Applied += ProfileManager_Applied;
            profileManager.Value.Discarded += ProfileManager_Discarded;
            // process existing profiles
            var fileEntries = Directory.GetFiles(ProfilesPath, "*.json", SearchOption.AllDirectories);
            foreach (var fileName in fileEntries)
                ProcessProfile(fileName);

            foreach (var devicePowerProfile in MainWindow.CurrentDevice.DevicePowerProfiles)
            {
                if (!profiles.ContainsKey(devicePowerProfile.Guid))
                    UpdateOrCreateProfile(devicePowerProfile, UpdateSource.Serializer);
            }

            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "PowerProfileManager");
        }

        public void Stop()
        {
            if (!IsInitialized)
                return;

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "PowerProfileManager");
        }

        private void LibreHardwareMonitor_CpuTemperatureChanged(float? value)
        {
            if (currentProfile is null || currentProfile.FanProfile is null || value is null)
                return;

            // update fan profile
            currentProfile.FanProfile.SetTemperature((float)value);

            switch (currentProfile.FanProfile.fanMode)
            {
                default:
                case FanMode.Hardware:
                    return;
                case FanMode.Software:
                    double fanSpeed = currentProfile.FanProfile.GetFanSpeed();
                    MainWindow.CurrentDevice.SetFanDuty(fanSpeed);
                    return;
            }
        }

        private void ProfileManager_Applied(Profile profile, UpdateSource source)
        {
            PowerProfile powerProfile = GetProfile(profile.PowerProfile);
            if (powerProfile is null)
                return;

            // update current profile
            currentProfile = powerProfile;

            Applied?.Invoke(powerProfile, source);
        }

        private void ProfileManager_Discarded(Profile profile)
        {
            // reset current profile
            currentProfile = null;

            PowerProfile powerProfile = GetProfile(profile.PowerProfile);
            if (powerProfile is null)
                return;

            Discarded?.Invoke(powerProfile);
        }

        private void ProcessProfile(string fileName)
        {
            PowerProfile profile = null;

            try
            {
                var outputraw = File.ReadAllText(fileName);
                var jObject = JObject.Parse(outputraw);

                profile = JsonConvert.DeserializeObject<PowerProfile>(outputraw, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });
            }
            catch (Exception ex)
            {
                LogManager.LogError("Could not parse power profile {0}. {1}", fileName, ex.Message);
            }

            // failed to parse
            if (profile is null || profile.Name is null)
            {
                LogManager.LogError("Failed to parse power profile {0}", fileName);
                return;
            }

            UpdateOrCreateProfile(profile, UpdateSource.Serializer);
        }

        public void UpdateOrCreateProfile(PowerProfile profile, UpdateSource source)
        {
            // update database
            profiles[profile.Guid] = profile;

            // raise event
            Updated?.Invoke(profile, source);

            if (source == UpdateSource.Serializer)
                return;

            // warn owner
            bool isCurrent = profile.Guid == currentProfile?.Guid;

            if (isCurrent)
                Applied?.Invoke(profile, source);

            // serialize profile
            SerializeProfile(profile);
        }

        public bool Contains(Guid guid)
        {
            return profiles.ContainsKey(guid);
        }

        public bool Contains(PowerProfile profile)
        {
            return profiles.ContainsValue(profile);
        }

        public PowerProfile GetProfile(Guid guid)
        {
            if (profiles.TryGetValue(guid, out var profile))
                return profile;

            return null;
        }

        public PowerProfile GetCurrent()
        {
            if (currentProfile is not null)
                return currentProfile;

            return null;
        }

        public void SerializeProfile(PowerProfile profile)
        {
            // update profile version to current build
            profile.Version = new Version(MainWindow.fileVersionInfo.FileVersion);

            var jsonString = JsonConvert.SerializeObject(profile, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

            // prepare for writing
            var profilePath = Path.Combine(ProfilesPath, profile.GetFileName());

            try
            {
                if (FileUtils.IsFileWritable(profilePath))
                    File.WriteAllText(profilePath, jsonString);
            }
            catch { }
        }

        public void DeleteProfile(PowerProfile profile)
        {
            string profilePath = Path.Combine(ProfilesPath, profile.GetFileName());

            if (profiles.ContainsKey(profile.Guid))
            {
                profiles.Remove(profile.Guid);

                // warn owner
                bool isCurrent = profile.Guid == currentProfile?.Guid;

                // raise event
                Discarded?.Invoke(profile);

                // raise event(s)
                Deleted?.Invoke(profile);

                // send toast
                // todo: localize me
                toastManager.Value.SendToast($"Power Profile {profile.FileName} deleted");

                LogManager.LogInformation("Deleted power profile {0}", profilePath);
            }

            FileUtils.FileDelete(profilePath);
        }

        #region events
        public event DeletedEventHandler Deleted;
        public delegate void DeletedEventHandler(PowerProfile profile);

        public event UpdatedEventHandler Updated;
        public delegate void UpdatedEventHandler(PowerProfile profile, UpdateSource source);

        public event AppliedEventHandler Applied;
        public delegate void AppliedEventHandler(PowerProfile profile, UpdateSource source);

        public event DiscardedEventHandler Discarded;
        public delegate void DiscardedEventHandler(PowerProfile profile);

        public event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();
        #endregion
    }
}
