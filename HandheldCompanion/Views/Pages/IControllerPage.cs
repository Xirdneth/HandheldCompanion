using System;
using System.Windows;

namespace HandheldCompanion.Views.Pages
{
    public interface IControllerPage
    {
        void Init();
        void InitializeComponent();
        void Page_Closed();
        void SetTag(string Tag);

        event RoutedEventHandler ControllerPageLoaded;
    }
}