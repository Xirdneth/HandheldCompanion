using HandheldCompanion.Controllers;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface IMotionManager
    {
        event MotionManager.InitializedEventHandler Initialized;
        event MotionManager.OverlayModelEventHandler OverlayModelUpdate;
        event MotionManager.SettingsMode0EventHandler SettingsMode0Update;
        event MotionManager.SettingsMode1EventHandler SettingsMode1Update;

        double DeltaSeconds { get; set; }
        void Start();
        void Stop();
        void UpdateReport(ControllerState controllerState);
    }
}