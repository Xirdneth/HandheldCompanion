using HandheldCompanion.Controllers;
using SharpDX.XInput;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface IControllerManager
    {
        event ControllerManager.ControllerPluggedEventHandler ControllerPlugged;
        event ControllerManager.ControllerSelectedEventHandler ControllerSelected;
        event ControllerManager.ControllerUnpluggedEventHandler ControllerUnplugged;
        event ControllerManager.InitializedEventHandler Initialized;
        event ControllerManager.InputsUpdatedEventHandler InputsUpdated;
        event ControllerManager.WorkingEventHandler Working;

        ConcurrentDictionary<string, bool> PowerCyclers { get; set; }
        bool IsInitialized { get; set; }
        XInputController GetControllerFromSlot(UserIndex userIndex = UserIndex.One, bool physical = true);
        List<IController> GetControllers();
        IEnumerable<IController> GetPhysicalControllers();
        IController GetTargetController();
        IEnumerable<IController> GetVirtualControllers();
        bool HasPhysicalController();
        bool HasVirtualController();
        bool ResumeControllers();
        void SetTargetController(string baseContainerDeviceInstanceId, bool IsPowerCycling);

        IController GetEmulatedController();
        Task Start();
        void Stop();
        bool SuspendController(string baseContainerDeviceInstanceId);
    }
}