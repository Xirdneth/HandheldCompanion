using HandheldCompanion.Managers;
using HandheldCompanion.Platforms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace HandheldCompanion.Controls.Hints
{
    public class Hint_SteamNeptuneDesktop : IHint
    {
        private readonly Lazy<IPlatformManager> platformManager;

        public Hint_SteamNeptuneDesktop() : base()
        {
            this.platformManager = App.ServiceProvider.GetRequiredService<Lazy<IPlatformManager>>();
            platformManager.Value.Steam.Updated += Steam_Updated;
            platformManager.Value.Initialized += PlatformManager_Initialized;

            // default state
            this.HintActionButton.Visibility = Visibility.Visible;

            this.HintTitle.Text = Properties.Resources.Hint_SteamNeptuneDesktop;
            this.HintDescription.Text = Properties.Resources.Hint_SteamNeptuneDesktopDesc;
            this.HintReadMe.Text = Properties.Resources.Hint_SteamNeptuneReadme;

            this.HintActionButton.Content = Properties.Resources.Hint_SteamNeptuneAction;
            
        }

        private void Steam_Updated(PlatformStatus status)
        {
            bool DesktopProfileApplied = platformManager.Value.Steam.HasDesktopProfileApplied();

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (status)
                {
                    default:
                    case PlatformStatus.Stopping:
                    case PlatformStatus.Stopped:
                        this.Visibility = Visibility.Collapsed;
                        break;
                    case PlatformStatus.Started:
                        this.Visibility = DesktopProfileApplied ? Visibility.Visible : Visibility.Collapsed;
                        break;
                }
            });
        }

        private void PlatformManager_Initialized()
        {
            Steam_Updated(platformManager.Value.Steam.Status);
        }

        protected override void HintActionButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                platformManager.Value.Steam.StopProcess();

                while (platformManager.Value.Steam.IsRunning)
                    await Task.Delay(1000);

                platformManager.Value.Steam.StartProcess();
            });
        }

        public override void Stop()
        {
            base.Stop();
        }
    }
}
