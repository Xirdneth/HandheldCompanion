namespace HandheldCompanion.Managers.Interfaces
{
    public interface IOSDManager
    {
        event OSDManager.InitializedEventHandler Initialized;

        short OverlayLevel { get; set; }
        string Draw(int processId);
        void Start();
        void Stop();
    }
}