namespace HandheldCompanion.Platforms
{
    public interface IRTSS
    {
        event RTSS.HookedEventHandler Hooked;
        event RTSS.UnhookedEventHandler Unhooked;

        bool IsInstalled { get; set; }
        void Dispose();
        uint EnableFlag(uint flag, bool status);
        bool GetEnableOSD();
        double GetFramerate(int processId);
        bool GetProfileProperty<T>(string propertyName, out T value);
        void RequestFPS(int framerate);
        bool SetEnableOSD(bool enable);
        bool SetProfileProperty<T>(string propertyName, T value);
        bool Start();
        bool StartProcess();
        bool Stop(bool kill = false);
        bool StopProcess();
        void UpdateSettings();
    }
}