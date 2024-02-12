using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using iNKORE.UI.WPF.Modern.Controls;
using Nefarius.Utilities.DeviceManagement.PnP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.Pages;

/// <summary>
///     Interaction logic for ControllerSettings.xaml
/// </summary>
public partial class LayoutPage : Page, IILayoutPage
{
    private LayoutTemplate currentTemplate { get; set; }
    protected LockObject updateLock = new();

    // page vars
    private Dictionary<string, (ILayoutPage, NavigationViewItem)> pages;
    private ButtonsPage buttonsPage { get; set; }
    private DpadPage dpadPage { get; set; }
    private GyroPage gyroPage { get; set; }
    private JoysticksPage joysticksPage { get; set; }
    private TrackpadsPage trackpadsPage { get; set; }
    private TriggersPage triggersPage { get; set; }
    private readonly Lazy<ISettingsManager> settingsManager;
    private readonly Lazy<IProfileManager> profileManager;
    private readonly Lazy<ILayoutManager> layoutManager;
    private readonly Lazy<IControllerManager> controllerManager;
    private readonly Lazy<IVirtualManager> virtualManager;
    private readonly Lazy<IDeviceManager> deviceManager;
    private readonly Lazy<IHotkeysManager> hotkeysManager;
    private readonly Lazy<ITimerManager> timerManager;
    private readonly Lazy<IInputsManager> inputsManager;
    private NavigationView parentNavView;
    private string preNavItemTag;

    public LayoutPage(
        Lazy<ISettingsManager> settingsManager,
        Lazy<IProfileManager> profileManager,
        Lazy<ILayoutManager> layoutManager,
        Lazy<IControllerManager> controllerManager,
        Lazy<IVirtualManager> virtualManager,
        Lazy<IDeviceManager> deviceManager,
        Lazy<IHotkeysManager> hotkeysManager,
        Lazy<ITimerManager> timerManager,
        Lazy<IInputsManager> inputsManager)
    {
        this.settingsManager = settingsManager;
        this.profileManager = profileManager;
        this.layoutManager = layoutManager;
        this.controllerManager = controllerManager;
        this.virtualManager = virtualManager;
        this.deviceManager = deviceManager;
        this.hotkeysManager = hotkeysManager;
        this.timerManager = timerManager;
        this.inputsManager = inputsManager;

        InitializeComponent();
    }

    public void Init()
    {
        joysticksPage = new(controllerManager, timerManager);
        trackpadsPage = new(controllerManager, timerManager);
        triggersPage = new(controllerManager, timerManager);
        currentTemplate = new();
        buttonsPage = new(controllerManager, timerManager);
        dpadPage = new(controllerManager, timerManager);
        gyroPage = new(hotkeysManager, controllerManager, inputsManager, timerManager);

        // create controller related pages
        this.pages = new()
        {
            // buttons
            { "ButtonsPage", ( buttonsPage, navButtons ) },
            { "DpadPage", ( dpadPage, navDpad ) },

            // triger
            { "TriggersPage", ( triggersPage, navTriggers ) },

            // axis
            { "JoysticksPage", ( joysticksPage, navJoysticks ) },
            { "TrackpadsPage", ( trackpadsPage, navTrackpads ) },

            // gyro
            { "GyroPage", ( gyroPage, navGyro ) },
        };

        foreach (ButtonStack buttonStack in buttonsPage.ButtonStacks.Values.Union(dpadPage.ButtonStacks.Values).Union(triggersPage.ButtonStacks.Values).Union(joysticksPage.ButtonStacks.Values).Union(trackpadsPage.ButtonStacks.Values))
        {
            buttonStack.Updated += (sender, actions) => ButtonMapping_Updated((ButtonFlags)sender, actions);
            buttonStack.Deleted += (sender) => ButtonMapping_Deleted((ButtonFlags)sender);
        }

        foreach (TriggerMapping axisMapping in triggersPage.TriggerMappings.Values)
        {
            axisMapping.Updated += (sender, action) => AxisMapping_Updated((AxisLayoutFlags)sender, action);
            axisMapping.Deleted += (sender) => AxisMapping_Deleted((AxisLayoutFlags)sender);
        }

        foreach (AxisMapping axisMapping in joysticksPage.AxisMappings.Values.Union(trackpadsPage.AxisMappings.Values))
        {
            axisMapping.Updated += (sender, action) => AxisMapping_Updated((AxisLayoutFlags)sender, action);
            axisMapping.Deleted += (sender) => AxisMapping_Deleted((AxisLayoutFlags)sender);
        }

        foreach (GyroMapping gyroMapping in gyroPage.GyroMappings.Values)
        {
            gyroMapping.Updated += (sender, action) => AxisMapping_Updated((AxisLayoutFlags)sender, action);
            gyroMapping.Deleted += (sender) => AxisMapping_Deleted((AxisLayoutFlags)sender);
        }

        layoutManager.Value.Updated += LayoutManager_Updated;
        layoutManager.Value.Initialized += LayoutManager_Initialized;
        controllerManager.Value.ControllerSelected += ControllerManager_ControllerSelected;
        virtualManager.Value.ControllerSelected += VirtualManager_ControllerSelected;
        settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;
        deviceManager.Value.UsbDeviceArrived += DeviceManager_UsbDeviceUpdated;
        deviceManager.Value.UsbDeviceRemoved += DeviceManager_UsbDeviceUpdated;
        profileManager.Value.Updated += ProfileManager_Updated;
    }

    public void SetTag(string Tag)
    {
        this.Tag = Tag;
    }

    public void SetParentNavView(NavigationView parent)
    {
        this.parentNavView = parent;
    }
    private void ProfileManager_Updated(Profile profile, UpdateSource source, bool isCurrent)
    {
        if (!MainWindow.CurrentPageName.Equals("LayoutPage"))
            return;

        // update layout page if layout was updated elsewhere
        // good enough
        switch (source)
        {
            case UpdateSource.QuickProfilesPage:
                {
                    if (currentTemplate.Executable.Equals(profile.Executable))
                        UpdateLayout(profile.Layout);
                }
                break;
        }
    }

    private void ControllerManager_ControllerSelected(IController controller)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            RefreshLayoutList();

            // cascade update to (sub)pages
            foreach (var page in pages.Values)
            {
                page.Item1.UpdateController(controller);
                page.Item2.IsEnabled = page.Item1.IsEnabled();
            }
        });
    }

    private void DeviceManager_UsbDeviceUpdated(PnPDevice device, DeviceEventArgs obj)
    {
        IController controller = controllerManager.Value.GetTargetController();

        // lazy
        if (controller is not null)
            ControllerManager_ControllerSelected(controller);
    }

    // todo: fix me when migrated to NO-SERVICE
    private void VirtualManager_ControllerSelected(HIDmode HID)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // cascade update to (sub)pages
            foreach (var page in pages.Values)
                page.Item1.UpdateSelections();
        });
    }

    private void LayoutManager_Initialized()
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            RefreshLayoutList();
        });
    }

    private void LayoutManager_Updated(LayoutTemplate layoutTemplate)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // Get template separator index
            var idx = -1;

            // search if we already have this template listed
            foreach (var item in cB_Layouts.Items)
            {
                if (item is not ComboBoxItem)
                    continue;

                // get template
                var parent = (ComboBoxItem)item;
                if (parent.Content is not LayoutTemplate)
                    continue;

                var template = (LayoutTemplate)parent.Content;
                if (template.Guid.Equals(layoutTemplate.Guid))
                {
                    idx = cB_Layouts.Items.IndexOf(parent);
                    break;
                }
            }

            if (idx != -1)
            {
                // found it
                cB_Layouts.Items[idx] = new ComboBoxItem { Content = layoutTemplate };
            }
            else
            {
                // new entry
                if (layoutTemplate.IsInternal)
                    idx = cB_Layouts.Items.IndexOf(cB_LayoutsSplitterTemplates) + 1;
                else
                    idx = cB_Layouts.Items.IndexOf(cB_LayoutsSplitterCommunity) + 1;

                cB_Layouts.Items.Insert(idx, new ComboBoxItem { Content = layoutTemplate });
            }

            cB_Layouts.Items.Refresh();
            cB_Layouts.InvalidateVisual();
        });
    }

    private void SettingsManager_SettingValueChanged(string? name, object value)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            switch (name)
            {
                case "LayoutFilterOnDevice":
                    CheckBoxDeviceLayouts.IsChecked = Convert.ToBoolean(value);
                    RefreshLayoutList();
                    break;
            }
        });
    }

    private void RefreshLayoutList()
    {
        // Get filter settings
        var FilterOnDevice = settingsManager.Value.GetBoolean("LayoutFilterOnDevice");

        // Get current controller
        var controller = controllerManager.Value.GetTargetController();

        foreach (var layoutTemplate in layoutManager.Value.Templates)
        {
            // get parent
            if (layoutTemplate.Parent is not ComboBoxItem parent)
                continue;

            if (layoutTemplate.ControllerType is not null && FilterOnDevice)
                if (layoutTemplate.ControllerType != controller?.GetType())
                {
                    parent.Visibility = Visibility.Collapsed;
                    continue;
                }

            parent.Visibility = Visibility.Visible;
        }
    }

    private void ButtonMapping_Deleted(ButtonFlags button)
    {
        if (updateLock)
            return;

        currentTemplate.Layout.RemoveLayout(button);
    }

    private void ButtonMapping_Updated(ButtonFlags button, List<IActions> actions)
    {
        if (updateLock)
            return;

        currentTemplate.Layout.UpdateLayout(button, actions);
    }

    private void AxisMapping_Deleted(AxisLayoutFlags axis)
    {
        if (updateLock)
            return;

        currentTemplate.Layout.RemoveLayout(axis);
    }

    private void AxisMapping_Updated(AxisLayoutFlags axis, IActions action)
    {
        if (updateLock)
            return;

        currentTemplate.Layout.UpdateLayout(axis, action);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
    }

    public void Page_Closed()
    {
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        ((Expander)sender).BringIntoView();
    }

    public void UpdateLayout(Layout layout)
    {
        currentTemplate.Layout = layout;

        UpdatePages();
    }

    public void UpdateLayoutTemplate(LayoutTemplate layoutTemplate)
    {
        // TODO: Not entirely sure what is going on here, but the old templates were still sending
        // events. Shouldn't they be destroyed? Either there is a bug or I don't understand something
        // in C# (probably the latter). Either way this handles/fixes/workarounds the issue.
        if (layoutTemplate.Layout != currentTemplate.Layout)
            currentTemplate = layoutTemplate;

        UpdatePages();
    }

    private void UpdatePages()
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // This is a very important lock, it blocks backward events to the layout when
            // this is actually the backend that triggered the update. Notifications on higher
            // levels (pages and mappings) could potentially be blocked for optimization.
            using (new ScopedLock(updateLock))
            {
                // cascade update to (sub)pages
                foreach (var page in pages.Values)
                    page.Item1.Update(currentTemplate.Layout);

                // clear layout selection
                cB_Layouts.SelectedValue = null;

                CheckBoxDefaultLayout.IsChecked = currentTemplate.Layout.IsDefaultLayout;
                CheckBoxDefaultLayout.IsEnabled = currentTemplate.Layout != layoutManager.Value.GetDesktop();
            }
        });
    }

    private void cB_Layouts_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var comboBox = (ComboBox)sender;
        foreach (var item in comboBox.Items)
        {
            if (item is not ComboBoxItem)
                continue;

            var layoutTemplate = (ComboBoxItem)item;
            layoutTemplate.Width = comboBox.ActualWidth;
            layoutTemplate.InvalidateVisual();
        }
    }

    private async void ButtonApplyLayout_Click(object sender, RoutedEventArgs e)
    {
        if (cB_Layouts.SelectedItem is null)
            return;

        if (cB_Layouts.SelectedItem is not ComboBoxItem)
            return;

        // get parent
        var parent = cB_Layouts.SelectedItem as ComboBoxItem;

        if (parent.Content is not LayoutTemplate)
            return;

        // get template
        var layoutTemplate = (LayoutTemplate)parent.Content;

        var result = Dialog.ShowAsync(
            string.Format(Properties.Resources.ProfilesPage_AreYouSureApplyTemplate1, currentTemplate.Name),
            string.Format(Properties.Resources.ProfilesPage_AreYouSureApplyTemplate2, currentTemplate.Name),
            ContentDialogButton.Primary,
            $"{Properties.Resources.ProfilesPage_Cancel}",
            $"{Properties.Resources.ProfilesPage_Yes}", string.Empty, MainWindow.GetCurrent());

        await result; // sync call

        switch (result.Result)
        {
            case ContentDialogResult.Primary:
                {
                    // do not overwrite currentTemplate and currentTemplate.Layout as a whole
                    // because they both have important Update notifitications set
                    var newLayout = layoutTemplate.Layout.Clone() as Layout;
                    currentTemplate.Layout.AxisLayout = newLayout.AxisLayout;
                    currentTemplate.Layout.ButtonLayout = newLayout.ButtonLayout;
                    currentTemplate.Layout.GyroLayout = newLayout.GyroLayout;

                    currentTemplate.Name = layoutTemplate.Name;
                    currentTemplate.Description = layoutTemplate.Description;
                    currentTemplate.Guid = layoutTemplate.Guid; // not needed

                    // the whole layout has been updated without notification, trigger one
                    currentTemplate.Layout.UpdateLayout();

                    UpdatePages();
                }
                break;
        }
    }

    private void cB_Layouts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ButtonApplyLayout.IsEnabled = cB_Layouts.SelectedIndex != -1;
    }

    private void CheckBoxDeviceLayouts_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        settingsManager.Value.SetProperty("LayoutFilterOnDevice", CheckBoxDeviceLayouts.IsChecked);
    }

    private void LayoutExportButton_Click(object sender, RoutedEventArgs e)
    {
        LayoutTemplate newLayout = new()
        {
            Layout = currentTemplate.Layout,
            Name = LayoutTitle.Text,
            Description = LayoutDescription.Text,
            Author = LayoutAuthor.Text,
            Executable = SaveGameInfo.IsChecked == true ? currentTemplate.Executable : "",
            Product = SaveGameInfo.IsChecked == true ? currentTemplate.Product : "",
            IsInternal = false
        };

        if (newLayout.Name == string.Empty)
        {
            LayoutFlyout.Hide();
            _ = Dialog.ShowAsync("Layout template name cannot be empty",
                "Layout was not exported.",
                ContentDialogButton.Primary, null, $"{Properties.Resources.ProfilesPage_OK}", string.Empty, MainWindow.GetCurrent());
            return;
        }

        if (ExportForCurrent.IsChecked == true)
            newLayout.ControllerType = controllerManager.Value.GetTargetController()?.GetType();

        layoutManager.Value.SerializeLayoutTemplate(newLayout);

        LayoutFlyout.Hide();
        _ = Dialog.ShowAsync("Layout template exported",
            $"{newLayout.Name} was exported.",
            ContentDialogButton.Primary, null, $"{Properties.Resources.ProfilesPage_OK}", string.Empty, MainWindow.GetCurrent());
    }

    private void Flyout_Opening(object sender, object e)
    {
        if (currentTemplate.Executable == string.Empty && currentTemplate.Product == string.Empty)
            SaveGameInfo.IsChecked = SaveGameInfo.IsEnabled = false;
        else
            SaveGameInfo.IsChecked = SaveGameInfo.IsEnabled = true;

        var separator = currentTemplate.Name.Length > 0 && currentTemplate.Product.Length > 0 ? " - " : "";

        LayoutTitle.Text = $"{currentTemplate.Name}{separator}{currentTemplate.Product}";
        LayoutDescription.Text = currentTemplate.Description;
        LayoutAuthor.Text = currentTemplate.Author;
    }

    private void SaveGameInfo_Toggled(object sender, RoutedEventArgs e)
    {
        if (SaveGameInfo.IsChecked == true)
        {
            var separator = currentTemplate.Name.Length > 0 && currentTemplate.Product.Length > 0 ? " - " : "";
            LayoutTitle.Text = $"{currentTemplate.Name}{separator}{currentTemplate.Product}";
        }
        else
        {
            LayoutTitle.Text = $"{currentTemplate.Name}";
        }
    }

    private void LayoutCancelButton_Click(object sender, RoutedEventArgs e)
    {
        LayoutFlyout.Hide();
    }

    private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is null)
            return;

        NavigationViewItem navItem = (NavigationViewItem)args.InvokedItemContainer;
        preNavItemTag = (string)navItem.Tag;
        NavView_Navigate(preNavItemTag);
    }

    public void NavView_Navigate(string navItemTag)
    {
        var item = pages.FirstOrDefault(p => p.Key.Equals(navItemTag));
        Page _page = item.Value.Item1;

        // Get the page type before navigation so you can prevent duplicate
        // entries in the backstack.
        var preNavPageType = ContentFrame.CurrentSourcePageType;

        // Only navigate if the selected page isn't currently loaded.
        if (!(_page is null) && !Equals(preNavPageType, _page)) NavView_Navigate(_page);
    }

    public void NavView_Navigate(Page _page)
    {
        ContentFrame.Navigate(_page);
    }

    private void navView_Loaded(object sender, RoutedEventArgs e)
    {
        // Add handler for ContentFrame navigation.
        ContentFrame.Navigated += On_Navigated;

        // NavView doesn't load any page by default, so load home page.
        navView.SelectedItem = navView.MenuItems[0];

        // If navigation occurs on SelectionChanged, this isn't needed.
        // Because we use ItemInvoked to navigate, we need to call Navigate
        // here to load the home page.
        preNavItemTag = "ButtonsPage";
        NavView_Navigate(preNavItemTag);
    }

    private void On_Navigated(object sender, NavigationEventArgs e)
    {
        navView.IsBackEnabled = ContentFrame.CanGoBack;

        if (ContentFrame.SourcePageType is not null)
        {
            var preNavPageType = ContentFrame.CurrentSourcePageType;
            var preNavPageName = preNavPageType.Name;

            var NavViewItem = navView.MenuItems
                .OfType<NavigationViewItem>().FirstOrDefault(n => n.Tag.Equals(preNavPageName));

            if (!(NavViewItem is null))
                navView.SelectedItem = NavViewItem;

            string header = currentTemplate.Product.Length > 0 ?
                    "Profile: " + currentTemplate.Product : "Layout: Desktop";
            parentNavView.Header = new TextBlock() { Text = header };
        }
    }

    private void CheckBoxDefaultLayout_Checked(object sender, RoutedEventArgs e)
    {
        var isDefaultLayout = (bool)CheckBoxDefaultLayout.IsChecked;
        var prevDefaultLayoutProfile = profileManager.Value.GetProfileWithDefaultLayout();

        currentTemplate.Layout.IsDefaultLayout = isDefaultLayout;
        currentTemplate.Layout.UpdateLayout();

        // If option is enabled and a different default layout profile exists, we want to set option false on prev profile.
        if (isDefaultLayout && prevDefaultLayoutProfile != null && prevDefaultLayoutProfile.Layout != currentTemplate.Layout)
        {
            prevDefaultLayoutProfile.Layout.IsDefaultLayout = false;
            profileManager.Value.UpdateOrCreateProfile(prevDefaultLayoutProfile);
        }
    }
}