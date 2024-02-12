using HandheldCompanion.Inputs;

namespace HandheldCompanion.Managers
{
    public interface IInputsManager
    {
        bool IsInitialized { get; set; }
        bool IsListening { get; }

        event InputsManager.InitializedEventHandler Initialized;
        event InputsManager.TriggerRaisedEventHandler TriggerRaised;
        event InputsManager.TriggerUpdatedEventHandler TriggerUpdated;

        void ClearListening(Hotkey hotkey);
        void Start();
        void StartListening(Hotkey hotkey, InputsManager.ListenerType type);
        void InvokeTrigger(Hotkey hotkey, bool IsKeyDown, bool IsKeyUp);
        void Stop();
        void UpdateReport(ButtonState buttonState);
    }
}