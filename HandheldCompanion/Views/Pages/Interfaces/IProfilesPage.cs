namespace HandheldCompanion.Views.Pages.Interfaces
{
    public interface IProfilesPage
    {
        void Init();
        void InitializeComponent();
        void Page_Closed();
        void ProfileDeleted(Profile profile);
        void ProfileUpdated(Profile profile, UpdateSource source, bool isCurrent);
        void SetTag(string Tag);
        void SettingsManager_SettingValueChanged(string name, object value);
        void SubmitProfile(UpdateSource source = UpdateSource.ProfilesPage);
    }
}