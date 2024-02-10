using System;
using System.Windows;
using Page = System.Windows.Controls.Page;
using GameLib;
using HandheldCompanion.ViewModels;
using GameLib.Core;
using HandheldCompanion.Database;
using iNKORE.UI.WPF.Modern.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Forms;
using HandheldCompanion.Controls;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for LibraryPage.xaml
    /// </summary>
    public partial class LibraryPage : Page
    {
        private readonly GameViewModel _gameViewModel;

        public delegate void StatusChangedEventHandler(int status);
        public event StatusChangedEventHandler StatusChanged;


        public LibraryPage(GameViewModel gameViewModel)
        {
            InitializeComponent();
            this._gameViewModel = gameViewModel;
        }

        public LibraryPage(string Tag, LiteDbContext liteDb) : this(new GameViewModel(new LauncherManager(new LauncherOptions() { SearchExecutables = true }), liteDb))
        {
            this.Tag = Tag;          
            DataContext = _gameViewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {

        }
        private void Page_LayoutUpdated(object sender, EventArgs e)
        {
        }

        
    }
}
