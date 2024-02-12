namespace HandheldCompanion.Views.Pages
{
    public interface IOverlayPage
    {
        void InitializeComponent();
        void Page_Closed();

        void SetTag(string Tag);

        void Init();
    }
}