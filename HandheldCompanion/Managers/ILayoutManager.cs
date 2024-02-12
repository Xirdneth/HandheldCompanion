using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using System.Collections.Generic;
using System.IO;

namespace HandheldCompanion.Managers
{
    public interface ILayoutManager
    {
        FileSystemWatcher layoutWatcher { get; set; }

        event LayoutManager.InitializedEventHandler Initialized;
        event LayoutManager.UpdatedEventHandler Updated;

        List<LayoutTemplate> Templates { get; }
        Layout GetCurrent();
        Layout GetDesktop();
        ControllerState MapController(ControllerState controllerState);
        void SerializeLayout(Layout layout, string fileName);
        void SerializeLayoutTemplate(LayoutTemplate layoutTemplate);
        void Start();
        void Stop();
    }
}