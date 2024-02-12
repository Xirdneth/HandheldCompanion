namespace HandheldCompanion.Managers
{
    public interface IDynamicLightingManager
    {
        event DynamicLightingManager.InitializedEventHandler Initialized;

        void Start();
        void Stop();
    }
}