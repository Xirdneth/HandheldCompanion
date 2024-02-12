namespace HandheldCompanion.Processors
{
    public interface IProcessor
    {
        event Processor.InitializedEventHandler Initialized;
        event Processor.LimitChangedHandler LimitChanged;
        event Processor.GfxChangedHandler MiscChanged;
        event Processor.StatusChangedHandler StatusChanged;
        event Processor.ValueChangedHandler ValueChanged;

        Processor GetCurrent();
        void Initialize();
        void SetGPUClock(double clock, int result = 0);
        void SetTDPLimit(PowerType type, double limit, bool immediate = false, int result = 0);
        void Stop();
    }
}