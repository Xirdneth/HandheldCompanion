using Nefarius.Utilities.DeviceManagement.PnP;

namespace HandheldCompanion.Views.Pages
{
    public interface ISettingsPage
    {
        void Init();
        void InitializeComponent();
        void Page_Closed();
        void SetTag(string Tag);
        void UpdateDevice(PnPDevice device = null);
    }
}