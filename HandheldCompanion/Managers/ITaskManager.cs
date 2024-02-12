namespace HandheldCompanion.Managers
{
    public interface ITaskManager
    {
        bool IsInitialized { get; set; }

        event TaskManager.InitializedEventHandler Initialized;

        void Start(string Executable);
        void Stop();
    }
}