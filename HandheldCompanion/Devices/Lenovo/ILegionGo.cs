using HandheldCompanion.Inputs;
using HandheldCompanion.Utils;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HandheldCompanion.Devices
{
    public interface ILegionGo
    {
        bool IsOpen { get; }

        void Close();
        string GetGlyph(ButtonFlags button);
        bool IsReady();
        bool Open();
        Task SetFanFullSpeedAsync(bool enabled);
        bool SetLedBrightness(int brightness);
        bool SetLedColor(Color MainColor, Color SecondaryColor, DeviceUtils.LEDLevel level, int speed = 100);
        bool SetLedStatus(bool status);
    }
}