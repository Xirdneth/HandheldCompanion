using Nefarius.Utilities.DeviceManagement.PnP;

namespace HandheldCompanion.Views.Pages
{
    public interface IAboutPage
    {
        void Init();
        void InitializeComponent();
        void SetTag(string Tag);
        void UpdateDevice(PnPDevice device);
    }
}