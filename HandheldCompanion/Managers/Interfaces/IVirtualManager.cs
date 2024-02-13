using HandheldCompanion.Controllers;
using HandheldCompanion.Utils;
using Nefarius.ViGEm.Client;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface IVirtualManager
    {
        bool IsInitialized { get; set; }
        ViGEmClient vClient { get; set; }
        HIDmode HIDmode { get; set; }
        HIDstatus HIDstatus { get; set; }

        event VirtualManager.ControllerSelectedEventHandler ControllerSelected;
        event VirtualManager.HIDChangedEventHandler HIDchanged;
        event VirtualManager.InitializedEventHandler Initialized;
        event VirtualManager.VibrateEventHandler Vibrated;

        void Resume();
        void SetControllerMode(HIDmode mode);
        void SetControllerStatus(HIDstatus status);
        void Start();
        void Stop();
        void Suspend();
        void UpdateInputs(ControllerState controllerState);
    }
}