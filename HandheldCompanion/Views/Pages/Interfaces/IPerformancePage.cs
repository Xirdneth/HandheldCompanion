using System;

namespace HandheldCompanion.Views.Pages
{
    public interface IPerformancePage
    {
        void Init();
        void InitializeComponent();
        void Page_Closed();
        void SelectionChanged(Guid guid);
        void SetTag(string Tag);
        void SubmitProfile(UpdateSource source = UpdateSource.ProfilesPage);
    }
}