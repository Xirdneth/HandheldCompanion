using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Views.Classes;
using System;

namespace HandheldCompanion.Views
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : OverlayWindow
    {
        public SplashScreen(Lazy<IHotkeysManager> hotkeysManager): base(hotkeysManager)
        {
            InitializeComponent();
        }
    }
}
