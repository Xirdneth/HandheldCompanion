namespace HandheldCompanion.Managers.Interfaces
{
    public interface IGPUManager
    {
        event GPUManager.InitializedEventHandler Initialized;

        void Start();
        void Stop();
    }
}