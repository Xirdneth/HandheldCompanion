namespace HandheldCompanion.Managers
{
    public interface IGPUManager
    {
        event GPUManager.InitializedEventHandler Initialized;

        void Start();
        void Stop();
    }
}