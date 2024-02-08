using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HandheldCompanion.Controls.Hints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using Page = System.Windows.Controls.Page;
using GameLib;
using HandheldCompanion.ViewModels;
using HandheldCompanion.Controls;
using Microsoft.Extensions.DependencyModel;
using System.Diagnostics;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for LibraryPage.xaml
    /// </summary>
    public partial class LibraryPage : Page
    {
        public delegate void StatusChangedEventHandler(int status);
        public event StatusChangedEventHandler StatusChanged;

        private Timer timer;
        private int prevStatus = -1;

        public static LauncherManager _launcherManager { get; set; }
        public static GameViewModel _gameViewModel { get; set; }

        public LibraryPage()
        {
            InitializeComponent();
            _launcherManager = new LauncherManager();
            _gameViewModel = new GameViewModel(_launcherManager);
            DataContext = _gameViewModel;
            //if (Library.Children.Count == 0)
            Games.Children.Add(new GameView());

        }
        public LibraryPage(string Tag) : this()
        {
            this.Tag = Tag;

            timer = new(1000);
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                //bool hasAnyVisible = Notifications.Children.OfType<IHint>().Any(element => element.Visibility == Visibility.Visible);
                //NothingToSee.Visibility = hasAnyVisible ? Visibility.Collapsed : Visibility.Visible;

                //int status = Convert.ToInt32(hasAnyVisible);
                //if (status != prevStatus)
                //{
                //    StatusChanged?.Invoke(status);
                //    prevStatus = status;
                //}
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                //IEnumerable<IHint> notifications = Notifications.Children.OfType<IHint>();
                //foreach (IHint hint in notifications)
                //    hint.Stop();
            });
        }

        private void Page_LayoutUpdated(object sender, EventArgs e)
        {
            timer.Stop();
            timer.Start();
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
}
