using HandheldCompanion.Processors;
using System;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface IPerformanceManager
    {
        event PerformanceManager.EPPChangedEventHandler EPPChanged;
        event PerformanceManager.InitializedEventHandler Initialized;
        event PerformanceManager.PerfBoostModeChangedEventHandler PerfBoostModeChanged;
        event PerformanceManager.LimitChangedHandler PowerLimitChanged;
        event PerformanceManager.PowerModeChangedEventHandler PowerModeChanged;
        event PerformanceManager.ValueChangedHandler PowerValueChanged;
        event PerformanceManager.StatusChangedHandler ProcessorStatusChanged;

        Guid[] PowerModes { get; }
        Processor GetProcessor();
        void Start();
        void Stop();
    }
}