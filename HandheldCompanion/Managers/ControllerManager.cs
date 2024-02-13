using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Platforms;
using HandheldCompanion.Utils;
using HandheldCompanion.Views;
using HandheldCompanion.Views.Classes;
using HandheldCompanion.Views.Pages;
using HandheldCompanion.Views.Windows.Interfaces;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.UI;
using Windows.UI.ViewManagement;
using DeviceType = SharpDX.DirectInput.DeviceType;

namespace HandheldCompanion.Managers;

public class ControllerManager : IControllerManager
{
    private static readonly ConcurrentDictionary<string, IController> Controllers = new();
    public ConcurrentDictionary<string, bool> PowerCyclers { get; set; } = new();

    private Thread watchdogThread;
    private bool watchdogThreadRunning;
    private bool ControllerManagement;

    private bool ControllerManagementSuccess = false;
    private int ControllerManagementAttempts = 0;
    private const int ControllerManagementMaxAttempts = 3;

    private static readonly XInputController? emptyXInput = new();
    private DS4Controller? emptyDS4 { get; set; }
    private readonly Lazy<ISettingsManager> settingsManager;
    private readonly Lazy<ISensorsManager> sensorsManager;
    private readonly Lazy<ILayoutManager> layoutManager;
    private readonly Lazy<IMotionManager> motionManager;
    private readonly Lazy<IVirtualManager> virtualManager;
    private readonly Lazy<IDeviceManager> deviceManager;
    private readonly Lazy<IProcessManager> processManager;
    private readonly Lazy<IToastManager> toastManager;
    private readonly Lazy<IInputsManager> inputsManager;
    private readonly Lazy<ITimerManager> timerManager;
    private readonly Lazy<IControllerManager> controllerManager;
    private readonly Lazy<IXInputController> xInputController;
    private readonly IOverlayModel overlayModel;
    private IController? targetController;
    private FocusedWindow focusedWindows = FocusedWindow.None;
    private ProcessEx? foregroundProcess;
    private bool ControllerMuted;

    public bool IsInitialized { get; set; }

    public ControllerManager(
        Lazy<ISettingsManager> settingsManager,
         Lazy<ISensorsManager> sensorsManager,
        Lazy<ILayoutManager> layoutManager,
        Lazy<IMotionManager> motionManager,
        Lazy<IVirtualManager> virtualManager,
        Lazy<IDeviceManager> deviceManager,
        Lazy<IProcessManager> processManager,
        Lazy<IToastManager> toastManager,
        Lazy<IInputsManager> inputsManager,
        Lazy<ITimerManager> timerManager,
        Lazy<IControllerManager> controllerManager,
         Lazy<IXInputController> xInputController,
         IOverlayModel overlayModel)
    {
        watchdogThread = new Thread(watchdogThreadLoop);
        watchdogThread.IsBackground = true;
        this.settingsManager = settingsManager;
        this.sensorsManager = sensorsManager;
        this.layoutManager = layoutManager;
        this.motionManager = motionManager;
        this.virtualManager = virtualManager;
        this.deviceManager = deviceManager;
        this.processManager = processManager;
        this.toastManager = toastManager;
        this.inputsManager = inputsManager;
        this.timerManager = timerManager;
        this.controllerManager = controllerManager;
        this.xInputController = xInputController;
        this.overlayModel = overlayModel;
        XInputController? emptyXInput = new(deviceManager,timerManager,settingsManager,controllerManager);
        DS4Controller? emptyDS4 = new(controllerManager,settingsManager,timerManager);
}

    public Task Start()
    {
        // Flushing possible JoyShocks...
        JSL.JslDisconnectAndDisposeAll();

        deviceManager.Value.XUsbDeviceArrived += XUsbDeviceArrived;
        deviceManager.Value.XUsbDeviceRemoved += XUsbDeviceRemoved;

        deviceManager.Value.HidDeviceArrived += HidDeviceArrived;
        deviceManager.Value.HidDeviceRemoved += HidDeviceRemoved;

        deviceManager.Value.Initialized += DeviceManager_Initialized;

        settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;

        UIGamepad.GotFocus += GamepadFocusManager_GotFocus;
        UIGamepad.LostFocus += GamepadFocusManager_LostFocus;

        processManager.Value.ForegroundChanged += ProcessManager_ForegroundChanged;

        virtualManager.Value.Vibrated += VirtualManager_Vibrated;

        MainWindow.CurrentDevice.KeyPressed += CurrentDevice_KeyPressed;
        MainWindow.CurrentDevice.KeyReleased += CurrentDevice_KeyReleased;

        MainWindow.uiSettings.ColorValuesChanged += OnColorValuesChanged;

        // enable HidHide
        HidHide.SetCloaking(true);

        IsInitialized = true;
        Initialized?.Invoke();

        // summon an empty controller, used to feed Layout UI
        // todo: improve me
        ControllerSelected?.Invoke(GetEmulatedController());

        LogManager.LogInformation("{0} has started", "ControllerManager");

        return Task.CompletedTask;
    }

    public void Stop()
    {
        if (!IsInitialized)
            return;

        IsInitialized = false;

        // unplug on close
        ClearTargetController();

        deviceManager.Value.XUsbDeviceArrived -= XUsbDeviceArrived;
        deviceManager.Value.XUsbDeviceRemoved -= XUsbDeviceRemoved;

        deviceManager.Value.HidDeviceArrived -= HidDeviceArrived;
        deviceManager.Value.HidDeviceRemoved -= HidDeviceRemoved;

        settingsManager.Value.SettingValueChanged -= SettingsManager_SettingValueChanged;

        // uncloak on close, if requested
        if (settingsManager.Value.GetBoolean("HIDuncloakonclose"))
            foreach (var controller in GetPhysicalControllers())
                controller.Unhide(false);

        // Flushing possible JoyShocks...
        JSL.JslDisconnectAndDisposeAll();

        LogManager.LogInformation("{0} has stopped", "ControllerManager");
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        var _systemBackground = MainWindow.uiSettings.GetColorValue(UIColorType.Background);
        var _systemAccent = MainWindow.uiSettings.GetColorValue(UIColorType.Accent);

        targetController?.SetLightColor(_systemAccent.R, _systemAccent.G, _systemAccent.B);
    }

    [Flags]
    private enum FocusedWindow
    {
        None,
        MainWindow,
        Quicktools
    }

    private void GamepadFocusManager_LostFocus(Control control)
    {
        GamepadWindow gamepadWindow = (GamepadWindow)control;

        switch (gamepadWindow.Title)
        {
            case "QuickTools":
                focusedWindows &= ~FocusedWindow.Quicktools;
                break;
            default:
                focusedWindows &= ~FocusedWindow.MainWindow;
                break;
        }

        // check applicable scenarios
        CheckControllerScenario();
    }

    private void GamepadFocusManager_GotFocus(Control control)
    {
        GamepadWindow gamepadWindow = (GamepadWindow)control;
        switch (gamepadWindow.Title)
        {
            case "QuickTools":
                focusedWindows |= FocusedWindow.Quicktools;
                break;
            default:
                focusedWindows |= FocusedWindow.MainWindow;
                break;
        }

        // check applicable scenarios
        CheckControllerScenario();
    }

    private void ProcessManager_ForegroundChanged(ProcessEx processEx, ProcessEx backgroundEx)
    {
        foregroundProcess = processEx;

        // check applicable scenarios
        CheckControllerScenario();
    }

    private void CurrentDevice_KeyReleased(ButtonFlags button)
    {
        // calls current controller (if connected)
        var controller = GetTargetController();
        controller?.InjectButton(button, false, true);
    }

    private void CurrentDevice_KeyPressed(ButtonFlags button)
    {
        // calls current controller (if connected)
        var controller = GetTargetController();
        controller?.InjectButton(button, true, false);
    }

    private void CheckControllerScenario()
    {
        ControllerMuted = false;

        // platform specific scenarios
        if (foregroundProcess?.Platform == PlatformType.Steam)
        {
            // mute virtual controller if foreground process is Steam or Steam-related and user a toggle the mute setting
            // Controller specific scenarios
            if (targetController is SteamController)
            {
                SteamController steamController = (SteamController)targetController;
                if (steamController.IsVirtualMuted())
                    ControllerMuted = true;
            }
        }

        // either main window or quicktools are focused
        if (focusedWindows != FocusedWindow.None)
            ControllerMuted = true;
    }

    private void SettingsManager_SettingValueChanged(string name, object value)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            switch (name)
            {
                case "VibrationStrength":
                    uint VibrationStrength = Convert.ToUInt32(value);
                    targetController?.SetVibrationStrength(VibrationStrength, MainWindow.GetCurrent().IsLoaded);
                    break;

                case "SteamControllerMute":
                    {
                        IController target = GetTargetController();
                        if (target is null)
                            return;

                        if (target is not SteamController)
                            return;

                        bool Muted = Convert.ToBoolean(value);
                        ((SteamController)target).SetVirtualMuted(Muted);
                    }
                    break;

                case "ControllerManagement":
                    {
                        ControllerManagement = Convert.ToBoolean(value);
                        switch (ControllerManagement)
                        {
                            case true:
                                {
                                    if (!watchdogThreadRunning)
                                    {
                                        watchdogThreadRunning = true;

                                        watchdogThread = new Thread(watchdogThreadLoop);
                                        watchdogThread.IsBackground = true;
                                        watchdogThread.Start();
                                    }
                                }
                                break;
                            case false:
                                {
                                    // suspend watchdog
                                    if (watchdogThread is not null)
                                    {
                                        watchdogThreadRunning = false;
                                        // Ensure the thread has finished execution
                                        if (watchdogThread.IsAlive)
                                            watchdogThread.Join();
                                        watchdogThread = null;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case "LegionControllerPassthrough":
                    {
                        IController target = GetTargetController();
                        if (target is null)
                            return;

                        if (target is not LegionController)
                            return;

                        bool enabled = Convert.ToBoolean(value);
                        ((LegionController)target).SetPassthrough(enabled);
                    }
                    break;
            }
        });
    }

    private void DeviceManager_Initialized()
    {
        // search for last known controller and connect
        var path = settingsManager.Value.GetString("HIDInstancePath");

        if (Controllers.ContainsKey(path))
        {
            // last known controller still is plugged, set as target
            SetTargetController(path, false);
        }
        else if (HasPhysicalController())
        {
            // no known controller, connect to first available
            path = GetPhysicalControllers().FirstOrDefault().GetContainerInstancePath();
            SetTargetController(path, false);
        }
    }

    private void VirtualManager_Vibrated(byte LargeMotor, byte SmallMotor)
    {
        targetController?.SetVibration(LargeMotor, SmallMotor);
    }

    private async void HidDeviceArrived(PnPDetails details, DeviceEventArgs obj)
    {
        if (!details.isGaming)
            return;

        Controllers.TryGetValue(details.baseContainerDeviceInstanceId, out IController controller);

        // are we power cycling ?
        PowerCyclers.TryGetValue(details.baseContainerDeviceInstanceId, out bool IsPowerCycling);

        // JoyShockLibrary
        int connectedJoys = -1;
        int joyShockId = -1;
        JSL.JOY_SETTINGS settings = new();

        DateTime timeout = DateTime.Now.Add(TimeSpan.FromSeconds(4));
        while (DateTime.Now < timeout && connectedJoys == -1)
        {
            try
            {
                // JslConnect might raise an exception
                connectedJoys = JSL.JslConnectDevices();
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        if (connectedJoys > 0)
        {
            int[] joysHandle = new int[connectedJoys];
            JSL.JslGetConnectedDeviceHandles(joysHandle, connectedJoys);

            // scroll handles until we find matching device path
            foreach (int i in joysHandle)
            {
                settings = JSL.JslGetControllerInfoAndSettings(i);

                string joyShockpath = settings.path;
                string detailsPath = details.devicePath;

                if (detailsPath.Equals(joyShockpath, StringComparison.InvariantCultureIgnoreCase))
                {
                    joyShockId = i;
                    break;
                }
            }
        }

        // device found
        if (joyShockId != -1)
        {
            // use handle
            settings.playerNumber = joyShockId;

            JSL.JOY_TYPE joyShockType = (JSL.JOY_TYPE)JSL.JslGetControllerType(joyShockId);

            if (controller is not null)
            {
                ((JSController)controller).AttachDetails(details);
                ((JSController)controller).AttachJoySettings(settings);

                // hide new InstanceID (HID)
                if (controller.IsHidden())
                    controller.Hide(false);

                IsPowerCycling = true;
            }
            else
            {
                // UI thread (sync)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    switch (joyShockType)
                    {
                        case JSL.JOY_TYPE.DualSense:
                            controller = new DualSenseController(settings, details,controllerManager,settingsManager,timerManager);
                            break;
                        case JSL.JOY_TYPE.DualShock4:
                            controller = new DS4Controller(settings, details,controllerManager,settingsManager,timerManager);
                            break;
                        case JSL.JOY_TYPE.ProController:
                            controller = new ProController(settings, details,settingsManager,controllerManager,timerManager);
                            break;
                    }
                });
            }
        }
        else
        {
            // DInput
            var directInput = new DirectInput();
            int VendorId = details.VendorID;
            int ProductId = details.ProductID;

            // initialize controller vars
            Joystick joystick = null;

            // search for the plugged controller
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
            {
                try
                {
                    // Instantiate the joystick
                    var lookup_joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                    var SymLink = deviceManager.Value.SymLinkToInstanceId(lookup_joystick.Properties.InterfacePath,
                        obj.InterfaceGuid.ToString());

                    if (SymLink.Equals(details.SymLink, StringComparison.InvariantCultureIgnoreCase))
                    {
                        joystick = lookup_joystick;
                        break;
                    }
                }
                catch
                {
                }
            }

            if (joystick is not null)
            {
                // supported controller
                VendorId = joystick.Properties.VendorId;
                ProductId = joystick.Properties.ProductId;
            }
            else
            {
                // unsupported controller
                LogManager.LogError("Couldn't find matching DInput controller: VID:{0} and PID:{1}",
                    details.GetVendorID(), details.GetProductID());
            }

            if (controller is not null)
            {
                controller.AttachDetails(details);

                // hide new InstanceID (HID)
                if (controller.IsHidden())
                    controller.Hide(false);
                else
                    controller.Unhide(false);

                IsPowerCycling = true;
            }
            else
            {
                // UI thread (sync)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // search for a supported controller
                    switch (VendorId)
                    {
                        // STEAM
                        case 0x28DE:
                            {
                                switch (ProductId)
                                {
                                    // WIRED STEAM CONTROLLER
                                    case 0x1102:
                                        // MI == 0 is virtual keyboards
                                        // MI == 1 is virtual mouse
                                        // MI == 2 is controller proper
                                        // No idea what's in case of more than one controller connected
                                        if (details.GetMI() == 2)
                                            controller = new GordonController(details,settingsManager,controllerManager,timerManager);
                                        break;
                                    // WIRELESS STEAM CONTROLLER
                                    case 0x1142:
                                        // MI == 0 is virtual keyboards
                                        // MI == 1-4 are 4 controllers
                                        // TODO: The dongle registers 4 controller devices, regardless how many are
                                        // actually connected. There is no easy way to check for connection without
                                        // actually talking to each controller. Handle only the first for now.
                                        if (details.GetMI() == 1)
                                            controller = new GordonController(details, settingsManager,controllerManager,timerManager);
                                        break;

                                    // STEAM DECK
                                    case 0x1205:
                                        controller = new NeptuneController(details, settingsManager,controllerManager,timerManager);
                                        break;
                                }
                            }
                            break;

                        // NINTENDO
                        case 0x057E:
                            {
                                switch (ProductId)
                                {
                                    // Nintendo Wireless Gamepad
                                    case 0x2009:
                                        break;
                                }
                            }
                            break;

                        // LENOVO
                        case 0x17EF:
                            {
                                switch (ProductId)
                                {
                                    case 0x6184:
                                        break;
                                }
                            }
                            break;
                    }
                });
            }
        }

        // unsupported controller
        if (controller is null)
        {
            LogManager.LogError("Unsupported Generic controller: VID:{0} and PID:{1}", details.GetVendorID(),
                details.GetProductID());
            return;
        }

        while (!controller.IsReady && controller.IsConnected())
            await Task.Delay(250);

        // set (un)busy
        controller.IsBusy = false;

        // update or create controller
        var path = controller.GetContainerInstancePath();
        Controllers[path] = controller;

        LogManager.LogDebug("Generic controller {0} plugged", controller.ToString());

        // raise event
        ControllerPlugged?.Invoke(controller, IsPowerCycling);

        toastManager.Value.SendToast(controller.ToString(), "detected");

        // remove controller from powercyclers
        PowerCyclers.TryRemove(controller.GetContainerInstancePath(), out _);

        // new controller logic
        if (deviceManager.Value.IsInitialized)
        {
            if (controller.IsPhysical() && targetController is null)
                SetTargetController(controller.GetContainerInstancePath(), IsPowerCycling);

            if (targetController is not null)
            {
                Color _systemBackground = MainWindow.uiSettings.GetColorValue(UIColorType.Background);
                Color _systemAccent = MainWindow.uiSettings.GetColorValue(UIColorType.Accent);
                targetController.SetLightColor(_systemAccent.R, _systemAccent.G, _systemAccent.B);
            }
        }
    }

    private async void HidDeviceRemoved(PnPDetails details, DeviceEventArgs obj)
    {
        IController controller = null;

        DateTime timeout = DateTime.Now.Add(TimeSpan.FromSeconds(10));
        while (DateTime.Now < timeout && controller is null)
        {
            if (Controllers.TryGetValue(details.baseContainerDeviceInstanceId, out controller))
                break;

            await Task.Delay(100);
        }

        if (controller is null)
            return;

        // XInput controller are handled elsewhere
        if (controller is XInputController)
            return;

        if (controller is JSController)
            JSL.JslDisconnect(controller.GetUserIndex());

        // are we power cycling ?
        PowerCyclers.TryGetValue(details.baseContainerDeviceInstanceId, out bool IsPowerCycling);

        // unhide on remove 
        if (!IsPowerCycling)
        {
            controller.Unhide(false);

            // unplug controller, if needed
            if (GetTargetController()?.GetContainerInstancePath() == details.baseContainerDeviceInstanceId)
                ClearTargetController();
            else
                controller.Unplug();

            // controller was unplugged
            Controllers.TryRemove(details.baseContainerDeviceInstanceId, out _);
        }

        LogManager.LogDebug("Generic controller {0} unplugged", controller.ToString());

        // raise event
        ControllerUnplugged?.Invoke(controller, IsPowerCycling);
    }

    private void watchdogThreadLoop(object? obj)
    {
        while (watchdogThreadRunning)
        {
            // monitoring unexpected slot changes
            HashSet<byte> UserIndexes = new();
            bool XInputDrunk = false;

            foreach (XInputController xInputController in Controllers.Values.Where(c => c.Details is not null && c.Details.isXInput))
            {
                byte UserIndex = deviceManager.Value.GetXInputIndexAsync(xInputController.Details.baseContainerDevicePath);

                // controller is not ready yet
                if (UserIndex == byte.MaxValue)
                    continue;

                // that's not possible, XInput is drunk
                if (!UserIndexes.Add(UserIndex))
                    XInputDrunk = true;

                xInputController.AttachController(UserIndex);
            }

            if (XInputDrunk)
            {
                foreach (XInputController xInputController in Controllers.Values.Where(c => c.Details is not null && c.Details.isXInput))
                    xInputController.AttachController(byte.MaxValue);

                Thread.Sleep(2000);
            }

            if (virtualManager.Value.HIDmode == HIDmode.Xbox360Controller && virtualManager.Value.HIDstatus == HIDstatus.Connected)
            {
                if (HasVirtualController())
                {
                    // check if it is first controller
                    IController controller = GetControllerFromSlot(UserIndex.One, false);
                    if (controller is null)
                    {
                        // disable that setting if we failed too many times
                        if (ControllerManagementAttempts == ControllerManagementMaxAttempts)
                        {
                            settingsManager.Value.SetProperty("ControllerManagement", false);

                            // resume all physical controllers
                            StringCollection deviceInstanceIds = settingsManager.Value.GetStringCollection("SuspendedControllers");
                            if (deviceInstanceIds is not null && deviceInstanceIds.Count != 0)
                                ResumeControllers();

                            ControllerManagementSuccess = false;
                            ControllerManagementAttempts = 0;

                            Working?.Invoke(2);

                            // suspend watchdog
                            if (watchdogThread is not null)
                            {
                                watchdogThreadRunning = false;
                                // Ensure the thread has finished execution
                                if (watchdogThread.IsAlive)
                                    watchdogThread.Join();
                                watchdogThread = null;
                            }
                        }
                        else
                        {
                            Working?.Invoke(0);
                            ControllerManagementSuccess = false;
                            ControllerManagementAttempts++;

                            // suspend virtual controller
                            virtualManager.Value.Suspend();

                            // suspend all physical controllers
                            foreach (XInputController xInputController in GetPhysicalControllers().OfType<XInputController>())
                            {
                                xInputController.IsBusy = true;
                                SuspendController(xInputController.Details.baseContainerDeviceInstanceId);
                            }

                            // resume virtual controller
                            virtualManager.Value.Resume();

                            // resume all physical controllers
                            StringCollection deviceInstanceIds = settingsManager.Value.GetStringCollection("SuspendedControllers");
                            if (deviceInstanceIds is not null && deviceInstanceIds.Count != 0)
                                ResumeControllers();

                            // suspend and resume virtual controller
                            virtualManager.Value.Suspend();
                            virtualManager.Value.Resume();
                        }
                    }
                    else
                    {
                        // resume all physical controllers
                        StringCollection deviceInstanceIds = settingsManager.Value.GetStringCollection("SuspendedControllers");
                        if (deviceInstanceIds is not null && deviceInstanceIds.Count != 0)
                            ResumeControllers();

                        // give us one extra loop to make sure we're good
                        if (!ControllerManagementSuccess)
                        {
                            ControllerManagementSuccess = true;
                            Working?.Invoke(1);
                        }
                        else
                            ControllerManagementAttempts = 0;
                    }
                }
            }

            Thread.Sleep(2000);
        }
    }

    private async void XUsbDeviceArrived(PnPDetails details, DeviceEventArgs obj)
    {
        Controllers.TryGetValue(details.baseContainerDeviceInstanceId, out IController controller);

        // are we power cycling ?
        PowerCyclers.TryGetValue(details.baseContainerDeviceInstanceId, out bool IsPowerCycling);

        // get details passed UserIndex
        UserIndex userIndex = (UserIndex)details.XInputUserIndex;

        // device manager failed to retrieve actual userIndex
        // use backup method
        if (userIndex == UserIndex.Any)
            userIndex = xInputController.Value.TryGetUserIndex(details);

        if (controller is not null)
        {
            ((XInputController)controller).AttachDetails(details);
            ((XInputController)controller).AttachController((byte)userIndex);

            // hide new InstanceID (HID)
            if (controller.IsHidden())
                controller.Hide(false);
            else
                controller.Unhide(false);

            IsPowerCycling = true;
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (details.GetVendorID())
                {
                    default:
                        controller = new XInputController(details);
                        break;

                    // LegionGo
                    case "0x17EF":
                        controller = new LegionController(details,deviceManager,timerManager,settingsManager,controllerManager);
                        break;
                }
            });
        }

        while (!controller.IsReady && controller.IsConnected())
            await Task.Delay(250);

        // set (un)busy
        controller.IsBusy = false;

        // update or create controller
        string path = details.baseContainerDeviceInstanceId;
        Controllers[path] = controller;

        LogManager.LogDebug("XInput controller {0} plugged", controller.ToString());

        // raise event
        ControllerPlugged?.Invoke(controller, IsPowerCycling);

        toastManager.Value.SendToast(controller.ToString(), "detected");

        // remove controller from powercyclers
        PowerCyclers.TryRemove(controller.GetContainerInstancePath(), out _);

        // new controller logic
        if (deviceManager.Value.IsInitialized)
        {
            if (controller.IsPhysical() && targetController is null)
                SetTargetController(controller.GetContainerInstancePath(), IsPowerCycling);

            if (targetController is not null)
            {
                Color _systemBackground = MainWindow.uiSettings.GetColorValue(UIColorType.Background);
                Color _systemAccent = MainWindow.uiSettings.GetColorValue(UIColorType.Accent);
                targetController.SetLightColor(_systemAccent.R, _systemAccent.G, _systemAccent.B);

                string ManufacturerName = MotherboardInfo.Manufacturer.ToUpper();
                switch (ManufacturerName)
                {
                    case "AOKZOE":
                    case "ONE-NETBOOK TECHNOLOGY CO., LTD.":
                    case "ONE-NETBOOK":
                        targetController.Rumble();
                        break;
                }
            }
        }
    }

    private async void XUsbDeviceRemoved(PnPDetails details, DeviceEventArgs obj)
    {
        IController controller = null;

        DateTime timeout = DateTime.Now.Add(TimeSpan.FromSeconds(10));
        while (DateTime.Now < timeout && controller is null)
        {
            if (Controllers.TryGetValue(details.baseContainerDeviceInstanceId, out controller))
                break;

            await Task.Delay(100);
        }

        if (controller is null)
            return;

        // are we power cycling ?
        PowerCyclers.TryGetValue(details.baseContainerDeviceInstanceId, out bool IsPowerCycling);

        // controller was unplugged
        if (!IsPowerCycling)
        {
            controller.Unhide(false);
            Controllers.TryRemove(details.baseContainerDeviceInstanceId, out _);

            // controller is current target
            if (targetController?.GetContainerInstancePath() == details.baseContainerDeviceInstanceId)
                ClearTargetController();
            else
                controller.Unplug();
        }

        LogManager.LogDebug("XInput controller {0} unplugged", controller.ToString());

        // raise event
        ControllerUnplugged?.Invoke(controller, IsPowerCycling);
    }

    private object targetLock = new object();

    private void ClearTargetController()
    {
        lock (targetLock)
        {
            // unplug previous controller
            if (targetController is not null)
            {
                targetController.InputsUpdated -= UpdateInputs;
                targetController.SetLightColor(0, 0, 0);
                targetController.Unplug();
                targetController = null;

                // update HIDInstancePath
                settingsManager.Value.SetProperty("HIDInstancePath", string.Empty);
            }
        }
    }

    public void SetTargetController(string baseContainerDeviceInstanceId, bool IsPowerCycling)
    {
        lock (targetLock)
        {
            // look for new controller
            if (!Controllers.TryGetValue(baseContainerDeviceInstanceId, out IController controller))
                return;

            if (controller.IsVirtual())
                return;

            // clear current target
            ClearTargetController();

            // update target controller
            targetController = controller;
            targetController.InputsUpdated += UpdateInputs;
            targetController.Plug();

            Color _systemBackground = MainWindow.uiSettings.GetColorValue(UIColorType.Background);
            Color _systemAccent = MainWindow.uiSettings.GetColorValue(UIColorType.Accent);
            targetController.SetLightColor(_systemAccent.R, _systemAccent.G, _systemAccent.B);

            // update HIDInstancePath
            settingsManager.Value.SetProperty("HIDInstancePath", baseContainerDeviceInstanceId);

            if (!IsPowerCycling)
            {
                if (settingsManager.Value.GetBoolean("HIDcloakonconnect"))
                {
                    bool powerCycle = true;

                    if (targetController is LegionController)
                    {
                        // todo:    Look for a byte within hid report that'd tend to mean both controllers are synced.
                        //          Then I guess we could try and power cycle them.
                        powerCycle = !((LegionController)targetController).IsWireless;
                    }

                    if (!targetController.IsHidden())
                        targetController.Hide(powerCycle);
                }
            }

            // check applicable scenarios
            CheckControllerScenario();

            // check if controller is about to power cycle
            PowerCyclers.TryGetValue(baseContainerDeviceInstanceId, out IsPowerCycling);

            if (!IsPowerCycling)
            {
                if (settingsManager.Value.GetBoolean("HIDvibrateonconnect"))
                    targetController.Rumble();
            }

            ControllerSelected?.Invoke(targetController);
        }
    }

    public bool SuspendController(string baseContainerDeviceInstanceId)
    {
        // PnPUtil.StartPnPUtil(@"/delete-driver C:\Windows\INF\xusb22.inf /uninstall /force");
        StringCollection deviceInstanceIds = settingsManager.Value.GetStringCollection("SuspendedControllers");

        if (deviceInstanceIds is null)
            deviceInstanceIds = new();

        try
        {
            PnPDevice pnPDevice = PnPDevice.GetDeviceByInstanceId(baseContainerDeviceInstanceId);
            UsbPnPDevice usbPnPDevice = pnPDevice.ToUsbPnPDevice();
            DriverMeta pnPDriver = null;

            try
            {
                pnPDriver = pnPDevice.GetCurrentDriver();
            }
            catch { }

            string enumerator = pnPDevice.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName);
            switch (enumerator)
            {
                case "USB":
                    if (pnPDriver is not null)
                    {
                        pnPDevice.InstallNullDriver(out bool rebootRequired);
                        usbPnPDevice.CyclePort();
                    }

                    if (!deviceInstanceIds.Contains(baseContainerDeviceInstanceId))
                        deviceInstanceIds.Add(baseContainerDeviceInstanceId);

                    settingsManager.Value.SetProperty("SuspendedControllers", deviceInstanceIds);
                    PowerCyclers[baseContainerDeviceInstanceId] = true;
                    return true;
            }
        }
        catch { }


        return false;
    }

    public bool ResumeControllers()
    {
        // PnPUtil.StartPnPUtil(@"/add-driver C:\Windows\INF\xusb22.inf /install");
        StringCollection deviceInstanceIds = settingsManager.Value.GetStringCollection("SuspendedControllers");

        if (deviceInstanceIds is null || deviceInstanceIds.Count == 0)
            return true;

        foreach (string baseContainerDeviceInstanceId in deviceInstanceIds)
        {
            try
            {
                PnPDevice pnPDevice = PnPDevice.GetDeviceByInstanceId(baseContainerDeviceInstanceId);
                UsbPnPDevice usbPnPDevice = pnPDevice.ToUsbPnPDevice();
                DriverMeta pnPDriver = null;

                try
                {
                    pnPDriver = pnPDevice.GetCurrentDriver();
                }
                catch { }

                string enumerator = pnPDevice.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName);
                switch (enumerator)
                {
                    case "USB":
                        if (pnPDriver is null || pnPDriver.InfPath != "xusb22.inf")
                        {
                            pnPDevice.RemoveAndSetup();
                            pnPDevice.InstallCustomDriver("xusb22.inf", out bool rebootRequired);
                        }

                        if (deviceInstanceIds.Contains(baseContainerDeviceInstanceId))
                            deviceInstanceIds.Remove(baseContainerDeviceInstanceId);

                        settingsManager.Value.SetProperty("SuspendedControllers", deviceInstanceIds);
                        PowerCyclers.TryRemove(baseContainerDeviceInstanceId, out _);
                        return true;
                }
            }
            catch { }
        }

        return false;
    }

    public IController GetTargetController()
    {
        return targetController;
    }

    public bool HasPhysicalController()
    {
        return GetPhysicalControllers().Count() != 0;
    }

    public bool HasVirtualController()
    {
        return GetVirtualControllers().Count() != 0;
    }

    public IEnumerable<IController> GetPhysicalControllers()
    {
        return Controllers.Values.Where(a => !a.IsVirtual()).ToList();
    }

    public IEnumerable<IController> GetVirtualControllers()
    {
        return Controllers.Values.Where(a => a.IsVirtual()).ToList();
    }

    public XInputController GetControllerFromSlot(UserIndex userIndex = 0, bool physical = true)
    {
        return Controllers.Values.FirstOrDefault(c => c is XInputController && ((physical && c.IsPhysical()) || !physical && c.IsVirtual()) && c.GetUserIndex() == (int)userIndex) as XInputController;
    }

    public List<IController> GetControllers()
    {
        return Controllers.Values.ToList();
    }

    private void UpdateInputs(ControllerState controllerState)
    {
        ButtonState buttonState = controllerState.ButtonState.Clone() as ButtonState;

        // raise event
        InputsUpdated?.Invoke(controllerState);

        // pass inputs to Inputs manager
        inputsManager.Value.UpdateReport(buttonState);

        // pass to SensorsManager for sensors value reading
        DeviceUtils.SensorFamily sensorSelection = (DeviceUtils.SensorFamily)settingsManager.Value.GetInt("SensorSelection");
        switch (sensorSelection)
        {
            case DeviceUtils.SensorFamily.Windows:
            case DeviceUtils.SensorFamily.SerialUSBIMU:
                sensorsManager.Value.UpdateReport(controllerState);
                break;
        }

        // pass to MotionManager for calculations
        motionManager.Value.UpdateReport(controllerState);

        // pass inputs to Overlay Model
        overlayModel.UpdateReport(controllerState);

        // pass inputs to Layout manager
        controllerState = layoutManager.Value.MapController(controllerState);

        // controller is muted
        if (ControllerMuted)
        {
            ControllerState emptyState = new ControllerState();
            emptyState.ButtonState[ButtonFlags.Special] = controllerState.ButtonState[ButtonFlags.Special];

            controllerState = emptyState;
        }

        virtualManager.Value.UpdateInputs(controllerState);
    }

    public IController GetEmulatedController()
    {
        // get HIDmode for the selected profile (could be different than HIDmode in settings if profile has HIDmode)
        HIDmode HIDmode = HIDmode.NoController;

        // if profile is selected, get its HIDmode
        if (ProfilesPage.selectedProfile != null)
            HIDmode = ProfilesPage.selectedProfile.HID;

        // if profile HID is NotSelected, use HIDmode from settings
        if (HIDmode == HIDmode.NotSelected)
            HIDmode = (HIDmode)settingsManager.Value.GetInt("HIDmode", true);

        switch (HIDmode)
        {
            default:
            case HIDmode.NoController:
            case HIDmode.Xbox360Controller:
                return emptyXInput;

            case HIDmode.DualShock4Controller:
                return emptyDS4;
        }
    }

    #region events

    public event ControllerPluggedEventHandler ControllerPlugged;
    public delegate void ControllerPluggedEventHandler(IController Controller, bool IsPowerCycling);

    public event ControllerUnpluggedEventHandler ControllerUnplugged;
    public delegate void ControllerUnpluggedEventHandler(IController Controller, bool IsPowerCycling);

    public event ControllerSelectedEventHandler ControllerSelected;
    public delegate void ControllerSelectedEventHandler(IController Controller);

    public event InputsUpdatedEventHandler InputsUpdated;
    public delegate void InputsUpdatedEventHandler(ControllerState Inputs);

    public event WorkingEventHandler Working;
    public delegate void WorkingEventHandler(int status);

    public event InitializedEventHandler Initialized;
    public delegate void InitializedEventHandler();

    #endregion
}