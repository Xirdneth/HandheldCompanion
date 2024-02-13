using System.Collections.Generic;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface IHotkeysManager
    {
        event HotkeysManager.CommandExecutedEventHandler CommandExecuted;
        event HotkeysManager.HotkeyCreatedEventHandler HotkeyCreated;
        event HotkeysManager.HotkeyTypeCreatedEventHandler HotkeyTypeCreated;
        event HotkeysManager.HotkeyUpdatedEventHandler HotkeyUpdated;
        event HotkeysManager.InitializedEventHandler Initialized;

        SortedDictionary<ushort, Hotkey> Hotkeys { get; set; }
        void SerializeHotkey(Hotkey hotkey, bool overwrite = false);
        void Start();
        void Stop();

        void ClearHotkey(Hotkey hotkey);
        void TriggerRaised(string listener, InputsChord input, InputsHotkey.InputsHotkeyType type, bool IsKeyDown, bool IsKeyUp);
    }
}