using HandheldCompanion.Inputs;
using SharpDX.XInput;

namespace HandheldCompanion.Controllers
{
    public interface IXInputController
    {
        void AttachController(byte userIndex);
        void CyclePort();
        string GetGlyph(AxisFlags axis);
        string GetGlyph(AxisLayoutFlags axis);
        string GetGlyph(ButtonFlags button);
        void Hide(bool powerCycle = true);
        bool IsConnected();
        void Plug();
        void SetVibration(byte LargeMotor, byte SmallMotor);
        string ToString();
        UserIndex TryGetUserIndex(PnPDetails details);
        void Unhide(bool powerCycle = true);
        void Unplug();
        void UpdateInputs(long ticks, bool commit);
    }
}