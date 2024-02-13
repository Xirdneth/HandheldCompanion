namespace HandheldCompanion.Managers.Interfaces
{
    public interface IDynamicLightingManager
    {
        event DynamicLightingManager.InitializedEventHandler Initialized;

        void Start();
        void Stop();
    }
}