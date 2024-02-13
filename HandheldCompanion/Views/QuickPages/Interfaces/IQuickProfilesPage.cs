namespace HandheldCompanion.Views.QuickPages.Interfaces
{
    public interface IQuickProfilesPage
    {
        void Init();
        void InitializeComponent();
        void SetTag(string Tag);
        void SubmitProfile(UpdateSource source = UpdateSource.QuickProfilesPage);
    }
}