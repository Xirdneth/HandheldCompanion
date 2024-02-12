using PrecisionTiming;
using System;
using System.Diagnostics;

namespace HandheldCompanion.Managers;

public class TimerManager : ITimerManager
{
    public delegate void InitializedEventHandler();

    public delegate void TickEventHandler(long ticks);

    private const int MasterInterval = 10; // 100Hz
    private readonly PrecisionTimer MasterTimer;
    public Stopwatch Stopwatch { get; set; }

    public bool IsInitialized { get; set; }

    public TimerManager()
    {
        MasterTimer = new PrecisionTimer();
        MasterTimer.SetInterval(new Action(DoWork), MasterInterval, false, 0, TimerMode.Periodic, true);

        Stopwatch = new Stopwatch();
    }

    private void DoWork()
    {
        // if (Stopwatch.ElapsedTicks % MasterInterval == 0)
        Tick?.Invoke(Stopwatch.ElapsedTicks);
    }

    public event TickEventHandler Tick;

    public event InitializedEventHandler Initialized;

    public int GetPeriod()
    {
        return MasterInterval;
    }

    public float GetPeriodMilliseconds()
    {
        return (float)MasterInterval / 1000L;
    }

    public long GetTickCount()
    {
        return Stopwatch.ElapsedTicks;
    }

    public long GetTimestamp()
    {
        return Stopwatch.GetTimestamp();
    }

    public long GetElapsedSeconds()
    {
        return GetElapsedMilliseconds() * 1000L;
    }

    public long GetElapsedMilliseconds()
    {
        return Stopwatch.ElapsedMilliseconds;
    }

    public void Start()
    {
        if (IsInitialized)
            return;

        MasterTimer.Start();
        Stopwatch.Start();

        IsInitialized = true;
        Initialized?.Invoke();

        LogManager.LogInformation("{0} has started with Period set to {1}", "TimerManager", GetPeriod());
    }

    public void Stop()
    {
        if (!IsInitialized)
            return;

        IsInitialized = false;

        MasterTimer.Stop();
        Stopwatch.Stop();

        LogManager.LogInformation("{0} has stopped", "TimerManager");
    }

    public void Restart()
    {
        Stop();
        Start();
    }
}