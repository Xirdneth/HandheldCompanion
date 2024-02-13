namespace HandheldCompanion.Views.Pages.Interfaces
{
    public interface IOverlayPage
    {
        void InitializeComponent();
        void Page_Closed();

        void SetTag(string Tag);

        void Init();
    }
}