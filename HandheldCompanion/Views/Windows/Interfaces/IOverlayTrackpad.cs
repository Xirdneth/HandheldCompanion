namespace HandheldCompanion.Views.Windows.Interfaces
{
    public interface IOverlayTrackpad
    {
        double GetWindowsScaling();
        void Init();
        void InitializeComponent();
        void Close();

        void ToggleVisibility();
    }
}