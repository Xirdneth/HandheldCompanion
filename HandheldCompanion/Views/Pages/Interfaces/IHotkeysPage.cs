namespace HandheldCompanion.Views.Pages
{
    public interface IHotkeysPage
    {
        void Init();
        void InitializeComponent();
        void Page_Closed();
        void SetTag(string Tag);
    }
}