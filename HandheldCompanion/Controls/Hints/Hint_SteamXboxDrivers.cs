using HandheldCompanion.Managers;
using HandheldCompanion.Platforms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace HandheldCompanion.Controls.Hints
{
    public class Hint_SteamXboxDrivers : IHint
    {
        private readonly Lazy<IPlatformManager> platformManager;

        public Hint_SteamXboxDrivers() : base()
        {
            this.platformManager = App.ServiceProvider.GetRequiredService<Lazy<IPlatformManager>>();
            platformManager.Value.Steam.Updated += Steam_Updated;
            platformManager.Value.Initialized += PlatformManager_Initialized;

            // default state
            this.HintActionButton.Visibility = Visibility.Collapsed;

            this.HintTitle.Text = Properties.Resources.Hint_SteamXboxDrivers;
            this.HintDescription.Text = Properties.Resources.Hint_SteamXboxDriversDesc;
            this.HintReadMe.Text = Properties.Resources.Hint_SteamXboxDriversReadme;
           
        }

        private void Steam_Updated(PlatformStatus status)
        {
            CheckDrivers();
        }

        private void PlatformManager_Initialized()
        {
            CheckDrivers();
        }

        private void CheckDrivers()
        {
            bool HasXboxDriversInstalled = platformManager.Value.Steam.HasXboxDriversInstalled();

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.Visibility = HasXboxDriversInstalled ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        public override void Stop()
        {
            base.Stop();
        }
    }
}
