using HandheldCompanion.Controllers;
using HandheldCompanion.Devices;
using HandheldCompanion.Sensors;
using HandheldCompanion.Views;
using Nefarius.Utilities.DeviceManagement.PnP;
using System;
using static HandheldCompanion.Utils.DeviceUtils;

namespace HandheldCompanion.Managers
{
    public class SensorsManager : ISensorsManager
    {
        private IMUGyrometer Gyrometer;
        private IMUAccelerometer Accelerometer;
        private SerialUSBIMU USBSensor;

        private SensorFamily sensorFamily;

        public bool IsInitialized;
        private readonly Lazy<ISettingsManager> settingsManager;
        private readonly Lazy<IDeviceManager> deviceManager;
        private readonly Lazy<IControllerManager> controllerManager;
        private readonly Lazy<ITimerManager> timerManager;
        private readonly Lazy<IMotionManager> motionManager;

        public event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();

        public SensorsManager(
            Lazy<ISettingsManager> settingsManager, 
            Lazy<IDeviceManager> deviceManager, 
            Lazy<IControllerManager> controllerManager, 
            Lazy<ITimerManager> timerManager,
            Lazy<IMotionManager> motionManager)
        {
            this.settingsManager = settingsManager;
            this.deviceManager = deviceManager;
            this.controllerManager = controllerManager;
            this.timerManager = timerManager;
            this.motionManager = motionManager;
            deviceManager.Value.UsbDeviceArrived += DeviceManager_UsbDeviceArrived;
            deviceManager.Value.UsbDeviceRemoved += DeviceManager_UsbDeviceRemoved;

            controllerManager.Value.ControllerSelected += ControllerManager_ControllerSelected;
            controllerManager.Value.ControllerUnplugged += ControllerManager_ControllerUnplugged;

            settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;

        }

        private void ControllerManager_ControllerSelected(IController Controller)
        {
            // select controller as current sensor if current sensor selection is none
            if (sensorFamily == SensorFamily.None && Controller.Capabilities.HasFlag(ControllerCapabilities.MotionSensor))
                settingsManager.Value.SetProperty("SensorSelection", (int)SensorFamily.Controller);
        }

        private void ControllerManager_ControllerUnplugged(IController Controller, bool IsPowerCycling)
        {
            if (sensorFamily != SensorFamily.Controller)
                return;

            // skip if controller isn't current or doesn't have motion sensor anyway
            if (!Controller.HasMotionSensor() || Controller != controllerManager.Value.GetTargetController())
                return;

            if (sensorFamily == SensorFamily.Controller)
                PickNextSensor();
        }

        private void DeviceManager_UsbDeviceRemoved(PnPDevice device, DeviceEventArgs obj)
        {
            if (USBSensor is null)
                return;

            // If the USB Gyro is unplugged, close serial connection
            USBSensor.Close();

            if (sensorFamily == SensorFamily.SerialUSBIMU)
                PickNextSensor();
        }

        private void PickNextSensor()
        {
            if (MainWindow.CurrentDevice.Capabilities.HasFlag(DeviceCapabilities.InternalSensor))
                settingsManager.Value.SetProperty("SensorSelection", (int)SensorFamily.Windows);
            else if (MainWindow.CurrentDevice.Capabilities.HasFlag(DeviceCapabilities.ExternalSensor))
                settingsManager.Value.SetProperty("SensorSelection", (int)SensorFamily.SerialUSBIMU);
            else if (controllerManager.Value.GetTargetController() is not null && controllerManager.Value.GetTargetController().HasMotionSensor())
                settingsManager.Value.SetProperty("SensorSelection", (int)SensorFamily.Controller);
            else
                settingsManager.Value.SetProperty("SensorSelection", (int)SensorFamily.None);
        }

        private void DeviceManager_UsbDeviceArrived(PnPDevice device, DeviceEventArgs obj)
        {
            // If USB Gyro is plugged, hook into it
            USBSensor = SerialUSBIMU.GetDefault();

            // select serial usb as current sensor if current sensor selection is none
            if (sensorFamily == SensorFamily.None)
                settingsManager.Value.SetProperty("SensorSelection", (int)SensorFamily.SerialUSBIMU);
        }

        private void SettingsManager_SettingValueChanged(string name, object value)
        {
            switch (name)
            {
                case "SensorPlacement":
                    {
                        SerialPlacement placement = (SerialPlacement)Convert.ToInt32(value);
                        USBSensor?.SetSensorPlacement(placement);
                    }
                    break;
                case "SensorPlacementUpsideDown":
                    {
                        bool upsidedown = Convert.ToBoolean(value);
                        USBSensor?.SetSensorOrientation(upsidedown);
                    }
                    break;
                case "SensorSelection":
                    {
                        SensorFamily sensorSelection = (SensorFamily)Convert.ToInt32(value);

                        // skip if set already
                        if (sensorFamily == sensorSelection)
                            return;

                        switch (sensorFamily)
                        {
                            case SensorFamily.Windows:
                                StopListening();
                                break;
                            case SensorFamily.SerialUSBIMU:
                                if (USBSensor is not null)
                                    USBSensor.Close();
                                break;
                            case SensorFamily.Controller:
                                break;
                        }

                        // update current sensorFamily
                        sensorFamily = sensorSelection;

                        switch (sensorFamily)
                        {
                            case SensorFamily.Windows:
                                break;
                            case SensorFamily.SerialUSBIMU:
                                {
                                    USBSensor = SerialUSBIMU.GetDefault();

                                    if (USBSensor is null)
                                        break;

                                    USBSensor.Open();

                                    SerialPlacement placement = (SerialPlacement)settingsManager.Value.GetInt("SensorPlacement");
                                    USBSensor.SetSensorPlacement(placement);
                                    bool upsidedown = settingsManager.Value.GetBoolean("SensorPlacementUpsideDown");
                                    USBSensor.SetSensorOrientation(upsidedown);
                                }
                                break;
                            case SensorFamily.Controller:
                                break;
                        }

                        SetSensorFamily(sensorSelection);
                    }
                    break;
            }
        }

        public void Start()
        {
            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "SensorsManager");
        }

        public void Stop()
        {
            if (!IsInitialized)
                return;

            StopListening();

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "SensorsManager");
        }

        public void Resume(bool update)
        {
            if (Gyrometer is not null)
                Gyrometer.UpdateSensor();

            if (Accelerometer is not null)
                Accelerometer.UpdateSensor();
        }

        private void StopListening()
        {
            // if required, halt gyrometer
            if (Gyrometer is not null)
                Gyrometer.StopListening();

            // if required, halt accelerometer
            if (Accelerometer is not null)
                Accelerometer?.StopListening();
        }

        public void UpdateReport(ControllerState controllerState)
        {
            switch (sensorFamily)
            {
                case SensorFamily.None:
                case SensorFamily.Controller:
                    return;
            }

            if (Gyrometer is not null)
                controllerState.GyroState.Gyroscope = Gyrometer.GetCurrentReading();

            if (Accelerometer is not null)
                controllerState.GyroState.Accelerometer = Accelerometer.GetCurrentReading();
        }

        public void SetSensorFamily(SensorFamily sensorFamily)
        {
            // initialize sensors
            var UpdateInterval = timerManager.Value.GetPeriod();

            Gyrometer = new IMUGyrometer(sensorFamily, UpdateInterval);
            Accelerometer = new IMUAccelerometer(sensorFamily, UpdateInterval,motionManager);
        }
    }
}
