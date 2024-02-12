using System.Diagnostics;

namespace HandheldCompanion.Managers
{
    public interface ITimerManager
    {
        bool IsInitialized { get; set; }
        Stopwatch Stopwatch { get; set; }

        event TimerManager.InitializedEventHandler Initialized;
        event TimerManager.TickEventHandler Tick;

        long GetElapsedMilliseconds();
        long GetElapsedSeconds();
        int GetPeriod();
        float GetPeriodMilliseconds();
        long GetTickCount();
        long GetTimestamp();
        void Restart();
        void Start();
        void Stop();
    }
}