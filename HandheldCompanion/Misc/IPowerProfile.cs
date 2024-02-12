using System;
using System.Windows.Controls;

namespace HandheldCompanion.Misc
{
    public interface IPowerProfile
    {
        bool AutoTDPEnabled { get; set; }
        float AutoTDPRequestedFPS { get; set; }
        int CPUBoostLevel { get; set; }
        int CPUCoreCount { get; set; }
        bool CPUCoreEnabled { get; set; }
        bool CPUOverrideEnabled { get; set; }
        double CPUOverrideValue { get; set; }
        bool Default { get; set; }
        string Description { get; set; }
        bool DeviceDefault { get; set; }
        bool EPPOverrideEnabled { get; set; }
        uint EPPOverrideValue { get; set; }
        FanProfile FanProfile { get; set; }
        string FileName { get; set; }
        bool GPUOverrideEnabled { get; set; }
        double GPUOverrideValue { get; set; }
        Guid Guid { get; set; }
        string Name { get; set; }
        int OEMPowerMode { get; set; }
        Guid OSPowerMode { get; set; }
        bool TDPOverrideEnabled { get; set; }
        double[] TDPOverrideValues { get; set; }
        Version Version { get; set; }

        void Check(Page page);
        void DrawUI(Page page);
        Button GetButton(Page page);
        string GetFileName();
        RadioButton GetRadioButton(Page page);
        bool IsDefault();
        string ToString();
        void Uncheck(Page page);
    }
}