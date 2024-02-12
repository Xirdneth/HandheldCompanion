using HandheldCompanion.Inputs;
using System.Windows.Forms;

namespace HandheldCompanion.Actions
{
    public interface IIActions
    {
        bool AutoRotate { get; set; }

        object Clone();
        void Execute(ButtonFlags button, bool value);
        void SetHaptic(ButtonFlags button, bool up);
        void SetOrientation(ScreenOrientation orientation);
    }
}