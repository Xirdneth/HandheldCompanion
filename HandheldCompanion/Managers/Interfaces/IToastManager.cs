namespace HandheldCompanion.Managers.Interfaces
{
    public interface IToastManager
    {
        bool IsEnabled { get; set; }
        bool IsInitialized { get; set; }

        void SendToast(string title, string content = "", string img = "Toast");
        void Start();
        void Stop();
    }
}