using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using HidLibrary;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace HandheldCompanion.Devices
{
    public interface IIDevice
    {
        Layout DefaultLayout { get; set; }
        Dictionary<byte, HidDevice> hidDevices { get; }

        string ProductIllustration { get; set; }
        bool IsOpen { get; }
        bool IsSupported { get; }
        IEnumerable<ButtonFlags> OEMButtons { get; }

        event IDevice.KeyPressedEventHandler KeyPressed;
        event IDevice.KeyReleasedEventHandler KeyReleased;
        event IDevice.PowerStatusChangedEventHandler PowerStatusChanged;

        void Close();
        bool ECRamDirectWrite(ushort address, ECDetails details, byte data);
        byte ECRamReadByte(ushort address);
        byte ECRamReadByte(ushort address, ECDetails details);
        bool ECRamWriteByte(ushort address, byte data);
        string GetButtonName(ButtonFlags button);
        IDevice GetDefault();
        FontIcon GetFontIcon(ButtonFlags button, int FontIconSize = 14);
        string GetGlyph(ButtonFlags button);
        bool HasKey();
        bool HasMotionSensor();
        bool IsReady();
        bool Open();
        void PullSensors();
        float ReadFanDuty();
        bool RestartSensor();
        void SetFanControl(bool enable, int mode = 0);
        void SetFanDuty(double percent);
        void SetKeyPressDelay(HIDmode controllerMode);
        bool SetLedBrightness(int brightness);
        bool SetLedColor(Color MainColor, Color SecondaryColor, DeviceUtils.LEDLevel level, int speed = 100);
        bool SetLedStatus(bool status);
    }
}