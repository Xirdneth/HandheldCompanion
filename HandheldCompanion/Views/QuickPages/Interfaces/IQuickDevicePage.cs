using HandheldCompanion.Managers.Desktop;

namespace HandheldCompanion.Views.QuickPages.Interfaces
{
    public interface IQuickDevicePage
    {
        void Init();
        void InitializeComponent();
        void SetResolution(ScreenResolution resolution);
        void SetTag(string Tag);
        void Close();
    }
}