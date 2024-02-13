using HandheldCompanion.Controllers;
using HandheldCompanion.Utils;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface ISensorsManager
    {
        event SensorsManager.InitializedEventHandler Initialized;

        void Resume(bool update);
        void SetSensorFamily(DeviceUtils.SensorFamily sensorFamily);
        void Start();
        void Stop();
        void UpdateReport(ControllerState controllerState);
    }
}