using System;

namespace HandheldCompanion.Views.QuickPages.Interfaces
{
    public interface IQuickPerformancePage
    {
        void Init();
        void InitializeComponent();
        void SelectionChanged(Guid guid);
        void SetTag(string Tag);
        void SubmitProfile(UpdateSource source = UpdateSource.ProfilesPage);
        void UpdateProfile();
    }
}