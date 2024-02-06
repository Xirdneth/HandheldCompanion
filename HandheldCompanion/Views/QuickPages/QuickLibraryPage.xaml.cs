using GameLib;
using GameLib.Core;
using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using static MS.WindowsAPICodePack.Internal.CoreNativeMethods;
using System.Windows.Interop;
using System.Diagnostics;
using HandheldCompanion.Views.Pages;
using HandheldCompanion.ViewModels;
using System.Threading.Tasks;
using craftersmine.SteamGridDBNet;

namespace HandheldCompanion.Views.QuickPages;

/// <summary>
///     Interaction logic for QuickLibraryPage.xaml
/// </summary>
public partial class QuickLibraryPage : Page
{
    public QuickLibraryPage(string Tag) : this()
    {
        this.Tag = Tag;
    }
    public static LauncherManager _launcherManager { get; set; }
    public static GameViewModel _gameViewModel { get; set; }

    public QuickLibraryPage()
    {
        
        InitializeComponent();
        _launcherManager = new LauncherManager();
        _gameViewModel = new GameViewModel(_launcherManager);
        DataContext = _gameViewModel;
        //if (Library.Children.Count == 0)
        Library.Children.Add(new GameView());

    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        
    }


    public static bool steamRunning()
    {
        Process[] pname = Process.GetProcessesByName("Steam");
        if (pname.Length != 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}
