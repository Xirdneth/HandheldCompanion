using HandheldCompanion.Controls;
using iNKORE.UI.WPF.Modern.Controls;

namespace HandheldCompanion.Views.Pages
{
    public interface IILayoutPage
    {
        void InitializeComponent();
        void NavView_Navigate(System.Windows.Controls.Page _page);
        void NavView_Navigate(string navItemTag);
        void Page_Closed();
        void SetParentNavView(NavigationView parent);
        void SetTag(string Tag);
        void UpdateLayout(Layout layout);
        void UpdateLayoutTemplate(LayoutTemplate layoutTemplate);
        void Init();
    }
}