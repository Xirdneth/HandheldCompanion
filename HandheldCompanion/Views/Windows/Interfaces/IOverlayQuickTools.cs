using System.Windows.Controls;

namespace HandheldCompanion.Views.Windows.Interfaces
{
    public interface IOverlayQuickTools
    {
        void Close(bool v);
        void Init();
        void InitializeComponent();
        void NavView_Navigate(Page _page);
        void NavView_Navigate(string navItemTag);
        void ToggleVisibility();
        void UpdateDefaultStyle();
        iNKORE.UI.WPF.Modern.Controls.Frame OverlayQuickToolsContentFrame { get; set; }
    }
}