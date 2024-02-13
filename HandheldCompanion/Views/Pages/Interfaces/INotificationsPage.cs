namespace HandheldCompanion.Views.Pages
{
    public interface INotificationsPage
    {
        event NotificationsPage.StatusChangedEventHandler StatusChanged;

        void Init();
        void InitializeComponent();
        void Page_Closed();
        void SetTag(string Tag);
    }
}