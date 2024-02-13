namespace HandheldCompanion.Views.Pages
{
    public interface IDevicePage
    {
        void Init();
        void InitializeComponent();
        void Page_Closed();
        void SetTag(string Tag);
    }
}