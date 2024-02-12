namespace HandheldCompanion.Platforms
{
    public interface ILibreHardwareMonitor
    {
        event LibreHardwareMonitor.ChangedHandler BatteryLevelChanged;
        event LibreHardwareMonitor.ChangedHandler BatteryPowerChanged;
        event LibreHardwareMonitor.ChangedHandler BatteryTimeSpanChanged;
        event LibreHardwareMonitor.ChangedHandler CPUClockChanged;
        event LibreHardwareMonitor.ChangedHandler CPULoadChanged;
        event LibreHardwareMonitor.ChangedHandler CPUPowerChanged;
        event LibreHardwareMonitor.ChangedHandler CPUTemperatureChanged;
        event LibreHardwareMonitor.ChangedHandler MemoryUsageChanged;

        public bool IsInstalled {get; set;}
        bool Start();
        bool Stop(bool kill = false);
    }
}