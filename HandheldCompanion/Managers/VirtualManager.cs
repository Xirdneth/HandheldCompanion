using HandheldCompanion.Controllers;
using HandheldCompanion.Targets;
using HandheldCompanion.Utils;
using Nefarius.ViGEm.Client;
using System;
using System.Threading;

namespace HandheldCompanion.Managers
{
    public class VirtualManager : IVirtualManager
    {
        // controllers vars
        public ViGEmClient vClient { get; set; }
        public ViGEmTarget vTarget;

        private DSUServer DSUServer;

        // settings vars
        public HIDmode HIDmode { get; set; }
        private HIDmode defaultHIDmode { get; set; }
        public HIDstatus HIDstatus { get; set; }

        public ushort ProductId = 0x28E; // Xbox 360
        public ushort VendorId = 0x45E;  // Microsoft

        public ushort FakeVendorId = 0x76B;  // HC

        public bool IsInitialized { get; set; }
        private readonly Lazy<ISettingsManager> settingsManager;
        private readonly Lazy<IProfileManager> profileManager;
        private readonly Lazy<IControllerManager> controllerManager;
        private readonly Lazy<IToastManager> toastManager;
        private readonly Lazy<ITimerManager> timerManager;
        private readonly Lazy<IVirtualManager> virtualManager;

        public event HIDChangedEventHandler HIDchanged;
        public delegate void HIDChangedEventHandler(HIDmode HIDmode);


        public event ControllerSelectedEventHandler ControllerSelected;
        public delegate void ControllerSelectedEventHandler(HIDmode mode);

        public event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();

        public event VibrateEventHandler Vibrated;
        public delegate void VibrateEventHandler(byte LargeMotor, byte SmallMotor);

        public VirtualManager(
            Lazy<ISettingsManager> settingsManager, 
            Lazy<IProfileManager> profileManager, 
            Lazy<IControllerManager> controllerManager,
            Lazy<IToastManager> toastManager,
            Lazy<ITimerManager> timerManager,
            Lazy<IVirtualManager> virtualManager)
        {
            this.settingsManager = settingsManager;
            this.profileManager = profileManager;
            this.controllerManager = controllerManager;
            this.toastManager = toastManager;
            this.timerManager = timerManager;
            this.virtualManager = virtualManager;
            HIDmode = HIDmode.NoController;
            defaultHIDmode = HIDmode.NoController;
            HIDstatus = HIDstatus.Disconnected;
            // verifying ViGEm is installed
            try
            {
                vClient = new ViGEmClient();
            }
            catch (Exception)
            {
                LogManager.LogCritical("ViGEm is missing. Please get it from: {0}", "https://github.com/ViGEm/ViGEmBus/releases");
                throw new InvalidOperationException();
            }

            // initialize DSUClient
            DSUServer = new DSUServer(timerManager);

            settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;
            settingsManager.Value.Initialized += SettingsManager_Initialized;
            profileManager.Value.Applied += ProfileManager_Applied;

        }

        public void Start()
        {
            // todo: improve me !!
            while (!controllerManager.Value.IsInitialized)
                Thread.Sleep(250);

            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "VirtualManager");
        }

        public void Stop()
        {
            if (!IsInitialized)
                return;

            ResetViGEm();
            DSUServer.Stop();

            // unsubscrive events
            profileManager.Value.Applied -= ProfileManager_Applied;

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "VirtualManager");
        }

        public void Resume()
        {
            // create new ViGEm client
            if (vClient is null)
                vClient = new ViGEmClient();

            // set controller mode
            SetControllerMode(HIDmode);
        }

        public void Suspend()
        {
            // reset vigem
            ResetViGEm();
        }

        private void SettingsManager_SettingValueChanged(string name, object value)
        {
            switch (name)
            {
                case "HIDmode":
                    defaultHIDmode = (HIDmode)Convert.ToInt32(value);
                    SetControllerMode(defaultHIDmode);
                    break;
                case "HIDstatus":
                    SetControllerStatus((HIDstatus)Convert.ToInt32(value));
                    break;
                case "DSUEnabled":
                    if (settingsManager.Value.IsInitialized)
                        SetDSUStatus(Convert.ToBoolean(value));
                    break;
                case "DSUport":
                    DSUServer.port = Convert.ToInt32(value);
                    if (settingsManager.Value.IsInitialized)
                        SetDSUStatus(settingsManager.Value.GetBoolean("DSUEnabled"));
                    break;
            }
        }

        private void ProfileManager_Applied(Profile profile, UpdateSource source)
        {
            try
            {
                // SetControllerMode takes care of ignoring identical mode switching
                if (HIDmode == profile.HID)
                    return;

                // todo: monitor ControllerManager and check if automatic controller management is running

                switch (profile.HID)
                {
                    case HIDmode.Xbox360Controller:
                    case HIDmode.DualShock4Controller:
                        {
                            SetControllerMode(profile.HID);
                            break;
                        }

                    default: // Default or not assigned
                        {
                            SetControllerMode(defaultHIDmode);
                            break;
                        }
                }
            }
            catch // TODO requires further testing
            {
                LogManager.LogError("Couldnt set per-profile HIDmode: {0}", profile.HID);
            }
        }


        private void SettingsManager_Initialized()
        {
            SetDSUStatus(settingsManager.Value.GetBoolean("DSUEnabled"));
        }

        private void SetDSUStatus(bool started)
        {
            if (started)
                DSUServer.Start();
            else
                DSUServer.Stop();
        }

        public void SetControllerMode(HIDmode mode)
        {
            // do not disconnect if similar to previous mode
            if (HIDmode == mode && vTarget is not null)
                return;

            // disconnect current virtual controller
            if (vTarget is not null)
            {
                vTarget.Disconnect();
                vTarget.Dispose();
                vTarget = null;
            }

            switch (mode)
            {
                default:
                case HIDmode.NoController:
                    if (vTarget is not null)
                    {
                        vTarget.Disconnect();
                        vTarget.Dispose();
                        vTarget = null;
                    }
                    break;
                case HIDmode.DualShock4Controller:
                    vTarget = new DualShock4Target(virtualManager, timerManager);
                    break;
                case HIDmode.Xbox360Controller:
                    // Generate a new random ProductId to help the controller pick empty slot rather than getting its previous one
                    VendorId = (ushort)new Random().Next(ushort.MinValue, ushort.MaxValue);
                    ProductId = (ushort)new Random().Next(ushort.MinValue, ushort.MaxValue);
                    vTarget = new Xbox360Target(VendorId, ProductId,timerManager, virtualManager);
                    break;
            }

            ControllerSelected?.Invoke(mode);

            // failed to initialize controller
            if (vTarget is null)
            {
                if (mode != HIDmode.NoController)
                    LogManager.LogError("Failed to initialise virtual controller with HIDmode: {0}", mode);
                return;
            }

            vTarget.Connected += OnTargetConnected;
            vTarget.Disconnected += OnTargetDisconnected;
            vTarget.Vibrated += OnTargetVibrated;

            // update status
            SetControllerStatus(HIDstatus);

            // update current HIDmode
            HIDmode = mode;
        }

        public void SetControllerStatus(HIDstatus status)
        {
            if (vTarget is null)
                return;

            switch (status)
            {
                default:
                case HIDstatus.Connected:
                    vTarget.Connect();
                    break;
                case HIDstatus.Disconnected:
                    vTarget.Disconnect();
                    break;
            }

            // update current HIDstatus
            HIDstatus = status;
        }

        private void OnTargetConnected(ViGEmTarget target)
        {
            toastManager.Value.SendToast($"{target}", "is now connected", $"HIDmode{(uint)target.HID}");
        }

        private void OnTargetDisconnected(ViGEmTarget target)
        {
            toastManager.Value.SendToast($"{target}", "is now disconnected", $"HIDmode{(uint)target.HID}");
        }

        private void OnTargetVibrated(byte LargeMotor, byte SmallMotor)
        {
            Vibrated?.Invoke(LargeMotor, SmallMotor);
        }

        public void UpdateInputs(ControllerState controllerState)
        {
            // DS4Touch is used by both targets below, update first
            DS4Touch.UpdateInputs(controllerState);

            vTarget?.UpdateInputs(controllerState);
            DSUServer?.UpdateInputs(controllerState);
        }

        private void ResetViGEm()
        {
            // dispose virtual controller
            if (vTarget is not null)
            {
                vTarget.Disconnect();
                vTarget.Dispose();
                vTarget = null;
            }

            // dispose ViGEm drivers
            if (vClient is not null)
            {
                vClient.Dispose();
                vClient = null;
            }
        }
    }
}