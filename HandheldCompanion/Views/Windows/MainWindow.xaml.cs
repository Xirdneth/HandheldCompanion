using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Devices;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.UI;
using HandheldCompanion.Utils;
using HandheldCompanion.Views.Classes;
using HandheldCompanion.Views.Pages;
using HandheldCompanion.Views.Pages.Interfaces;
using HandheldCompanion.Views.Windows.Interfaces;
using iNKORE.UI.WPF.Modern.Controls;
using Nefarius.Utilities.DeviceManagement.PnP;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
using Windows.Networking.NetworkOperators;
using Windows.UI.ViewManagement;
using static HandheldCompanion.Managers.InputsHotkey;
using Application = System.Windows.Application;
using Control = System.Windows.Controls.Control;
using Page = System.Windows.Controls.Page;
using RadioButton = System.Windows.Controls.RadioButton;

namespace HandheldCompanion.Views;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : GamepadWindow
{
    // devices vars
    public static IDevice CurrentDevice;

    // page vars
    private static readonly Dictionary<string, Page> _pages = new();
    private readonly Lazy<IOverlayPage> overlayPage;
    private readonly Lazy<IILayoutPage> layoutPage;
    private readonly Lazy<IProfilesPage> profilesPage;
    private readonly Lazy<IControllerPage> controllerPage;
    private readonly Lazy<INotificationsPage> notificationsPage;
    private readonly Lazy<IHotkeysPage> hotkeysPage;
    private readonly Lazy<IAboutPage> aboutPage;
    private readonly Lazy<IDevicePage> devicePage;
    private readonly Lazy<ISettingsPage> settingsPage;
    private readonly Lazy<IPerformancePage> performancePage;

    //Window vars
    private readonly Lazy<IOverlayQuickTools> overlayQuickTools;
    private readonly Lazy<IOverlayTrackpad> overlayTrackpad;
    private readonly Lazy<IOverlayModel> overlayModel;

    public static string CurrentExe, CurrentPath;

    private static MainWindow CurrentWindow;
    public static FileVersionInfo fileVersionInfo;
    private Assembly? CurrentAssembly;

    public static string InstallPath = string.Empty;
    public static string SettingsPath = string.Empty;
    public static string CurrentPageName = string.Empty;

    private bool appClosing;
    private bool IsReady;
    private readonly NotifyIcon notifyIcon;
    private readonly Lazy<IGPUManager> gPUManager;
    private readonly Lazy<IPowerProfileManager> powerProfileManager;
    private readonly Lazy<IProfileManager> profileManager;
    private readonly Lazy<IPerformanceManager> performanceManager;
    private readonly Lazy<IPlatformManager> platformManager;
    private readonly Lazy<ISettingsManager> settingsManager;
    private readonly Lazy<ILayoutManager> layoutManager;
    private readonly Lazy<IMotionManager> motionManager;
    private readonly Lazy<IHotkeysManager> hotkeysManager;
    private readonly Lazy<IXInputPlus> xInputPlus;
    private readonly Lazy<ISensorsManager> sensorsManager;
    private readonly Lazy<IOSDManager> oSDManager;
    private readonly Lazy<IControllerManager> controllerManager;
    private readonly Lazy<IVirtualManager> virtualManager;
    private readonly Lazy<IDynamicLightingManager> dynamicLightingManager;
    private readonly Lazy<IMultimediaManager> multimediaManager;
    private readonly Lazy<IUISounds> uISounds;
    private readonly Lazy<IIDevice> iDevice;
    private readonly Lazy<IProcessManager> processManager;
    private readonly Lazy<ITaskManager> taskManager;
    private readonly Lazy<IInputsManager> inputsManager;
    private readonly Lazy<IUpdateManager> updateManager;
    private readonly Lazy<IDeviceManager> deviceManager;
    private readonly Lazy<ISystemManager> systemManager;
    private readonly Lazy<IToastManager> toastManager;
    private readonly Lazy<ITimerManager> timerManager;
    private readonly Lazy<ILayoutTemplate> layoutTemplate;
    private bool NotifyInTaskbar;
    private string preNavItemTag;

    private WindowState prevWindowState;
    private SplashScreen splashScreen;

    public static UISettings uiSettings;

    private const int WM_QUERYENDSESSION = 0x0011;



    public MainWindow(
        Lazy<IGPUManager> gPUManager,
        Lazy<IPowerProfileManager> powerProfileManager,
        Lazy<IProfileManager> profileManager,
        Lazy<IPerformanceManager> performanceManager,
        Lazy<IPlatformManager> platformManager,
        Lazy<ISettingsManager> settingsManager,
        Lazy<ILayoutManager> layoutManager,
        Lazy<IMotionManager> motionManager,
        Lazy<IHotkeysManager> hotkeysManager,
        Lazy<IXInputPlus> xInputPlus,
        Lazy<ISensorsManager> sensorsManager,
        Lazy<IOSDManager> oSDManager,
        Lazy<IControllerManager> controllerManager,
        Lazy<IVirtualManager> virtualManager,
        Lazy<IDynamicLightingManager> dynamicLightingManager,
        Lazy<IMultimediaManager> multimediaManager,
        Lazy<IUISounds> uISounds,
        Lazy<IIDevice> iDevice,
        Lazy<IProcessManager> processManager,
        Lazy<ITaskManager> taskManager,
        Lazy<IInputsManager> inputsManager,
        Lazy<IUpdateManager> updateManager,
        Lazy<IDeviceManager> deviceManager,
        Lazy<ISystemManager> systemManager,
        Lazy<IToastManager> toastManager,
        Lazy<ITimerManager> timerManager,
        Lazy<IOverlayModel> overlayModel,
        Lazy<IOverlayPage> overlayPage,
        Lazy<IILayoutPage> layoutPage,
        Lazy<IProfilesPage> profilesPage,
        Lazy<IControllerPage> controllerPage,
        Lazy<INotificationsPage> notificationsPage,
        Lazy<IHotkeysPage> hotkeysPage,
        Lazy<IAboutPage> aboutPage,
        Lazy<IDevicePage> devicePage,
        Lazy<ISettingsPage> settingsPage,
        Lazy<IPerformancePage> performancePage,
        Lazy<IOverlayQuickTools> overlayQuickTools,
        Lazy<IOverlayTrackpad> overlayTrackpad)
    {
        var MainWindowStartup = Stopwatch.StartNew();
        this.gPUManager = gPUManager;
        this.powerProfileManager = powerProfileManager;
        this.profileManager = profileManager;
        this.performanceManager = performanceManager;
        this.platformManager = platformManager;
        this.settingsManager = settingsManager;
        this.layoutManager = layoutManager;
        this.motionManager = motionManager;
        this.hotkeysManager = hotkeysManager;
        this.xInputPlus = xInputPlus;
        this.sensorsManager = sensorsManager;
        this.oSDManager = oSDManager;
        this.controllerManager = controllerManager;
        this.virtualManager = virtualManager;
        this.dynamicLightingManager = dynamicLightingManager;
        this.multimediaManager = multimediaManager;
        this.uISounds = uISounds;
        this.iDevice = iDevice;
        this.processManager = processManager;
        this.taskManager = taskManager;
        this.inputsManager = inputsManager;
        this.updateManager = updateManager;
        this.deviceManager = deviceManager;
        this.systemManager = systemManager;
        this.toastManager = toastManager;
        this.timerManager = timerManager;
        

        //Pages
        this.overlayPage = overlayPage;
        this.layoutPage = layoutPage;
        this.profilesPage = profilesPage;
        this.controllerPage = controllerPage;
        this.notificationsPage = notificationsPage;
        this.hotkeysPage = hotkeysPage;
        this.aboutPage = aboutPage;
        this.devicePage = devicePage;
        this.settingsPage = settingsPage;
        this.performancePage = performancePage;


        //Windows
        this.overlayQuickTools = overlayQuickTools;
        this.overlayTrackpad = overlayTrackpad;
        this.overlayModel = overlayModel;

        InitializeComponent();

        CurrentAssembly = Assembly.GetExecutingAssembly();
        fileVersionInfo = FileVersionInfo.GetVersionInfo(CurrentAssembly.Location);
        CurrentWindow = this;
        GameOverlay.TimerService.EnableHighPrecisionTimers();

        // used by system manager, controller manager
        uiSettings = new UISettings();

        // used by gamepad navigation
        Tag = "MainWindow";

        // get process
        var process = Process.GetCurrentProcess();

        // fix touch support
        TabletDeviceCollection tabletDevices = Tablet.TabletDevices;

        // get first start
        bool FirstStart = settingsManager.Value.GetBoolean("FirstStart");

        // define current directory
        InstallPath = AppDomain.CurrentDomain.BaseDirectory;
        SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "HandheldCompanion");

        // initialiaze path
        if (!Directory.Exists(SettingsPath))
            Directory.CreateDirectory(SettingsPath);

        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        // initialize XInputWrapper
        xInputPlus.Value.ExtractXInputPlusLibraries();

        // initialize notifyIcon
        notifyIcon = new NotifyIcon
        {
            Text = Title,
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
            Visible = false,
            ContextMenuStrip = new ContextMenuStrip()
        };

        notifyIcon.DoubleClick += (sender, e) => { SwapWindowState(); };

        AddNotifyIconItem(Properties.Resources.MainWindow_MainWindow);
        AddNotifyIconItem(Properties.Resources.MainWindow_QuickTools);

        AddNotifyIconSeparator();

        AddNotifyIconItem(Properties.Resources.MainWindow_Exit);

        // paths
        CurrentExe = process.MainModule.FileName;
        CurrentPath = AppDomain.CurrentDomain.BaseDirectory;

        // initialize HidHide
        HidHide.RegisterApplication(CurrentExe);

        // initialize title
        Title += $" ({fileVersionInfo.FileVersion})";

        //iDevice.Value.Initialize(settingsManager,powerProfileManager,controllerManager,systemManager,timerManager);
        // initialize device
        CurrentDevice = iDevice.Value.GetDefault();
        CurrentDevice.PullSensors();

        // workaround for Bosch BMI320/BMI323 (as of 06/20/2023)
        // todo: check if still needed with Bosch G-sensor Driver V1.0.1.7
        // https://dlcdnets.asus.com/pub/ASUS/IOTHMD/Image/Driver/Chipset/34644/BoschG-sensor_ROG_Bosch_Z_V1.0.1.7_34644.exe?model=ROG%20Ally%20(2023)

        string currentDeviceType = CurrentDevice.GetType().Name;
        switch (currentDeviceType)
        {
            case "AYANEOAIRPlus":
            case "ROGAlly":
                {
                    LogManager.LogInformation("Restarting: {0}", CurrentDevice.InternalSensorName);

                    if (CurrentDevice.RestartSensor())
                    {
                        // give the device some breathing space once restarted
                        Thread.Sleep(500);

                        LogManager.LogInformation("Successfully restarted: {0}", CurrentDevice.InternalSensorName);
                    }
                    else
                        LogManager.LogError("Failed to restart: {0}", CurrentDevice.InternalSensorName);
                }
                break;

            case "SteamDeck":
                {
                    // prevent Steam Deck controller from being hidden by default
                    if (FirstStart)
                        settingsManager.Value.SetProperty("HIDcloakonconnect", false);
                }
                break;
        }

        // initialize splash screen on first start only
        if (FirstStart)
        {
            splashScreen = new SplashScreen(hotkeysManager);
            splashScreen.Show();

            settingsManager.Value.SetProperty("FirstStart", false);
        }

        // initialize UI sounds board
        var Sounds = uISounds.Value;


        var WindowLoadWatch = Stopwatch.StartNew();
        // load window(s)
        loadWindows();
        WindowLoadWatch.Stop();
        LogManager.LogInformation($"LoadWindows time Elapsed: {WindowLoadWatch.ElapsedMilliseconds}ms");

        var PagesLoadWatch = Stopwatch.StartNew();
        // load page(s)
        loadPages();
        PagesLoadWatch.Stop();
        LogManager.LogInformation($"LoadPages time Elapsed: {PagesLoadWatch.ElapsedMilliseconds}ms");

        // manage events
        inputsManager.Value.TriggerRaised += InputsManager_TriggerRaised;
        systemManager.Value.SystemStatusChanged += OnSystemStatusChanged;
        deviceManager.Value.UsbDeviceArrived += GenericDeviceUpdated;
        deviceManager.Value.UsbDeviceRemoved += GenericDeviceUpdated;
        controllerManager.Value.ControllerSelected += ControllerManager_ControllerSelected;
        virtualManager.Value.ControllerSelected += VirtualManager_ControllerSelected;

        toastManager.Value.Start();
        toastManager.Value.IsEnabled = settingsManager.Value.GetBoolean("ToastEnable");

        // start static managers in sequence
        gPUManager.Value.Start();
        powerProfileManager.Value.Start();
        profileManager.Value.Start();
        controllerManager.Value.Start();
        hotkeysManager.Value.Start();
        deviceManager.Value.Start();
        oSDManager.Value.Start();
        layoutManager.Value.Start();
        systemManager.Value.Start();
        dynamicLightingManager.Value.Start();
        multimediaManager.Value.Start();
        virtualManager.Value.Start();
        inputsManager.Value.Start();
        sensorsManager.Value.Start();
        timerManager.Value.Start();

        // todo: improve overall threading logic
        new Thread(() => { platformManager.Value.Start(); }).Start();
        new Thread(() => { processManager.Value.Start(); }).Start();
        new Thread(() => { taskManager.Value.Start(CurrentExe); }).Start();
        new Thread(() => { performanceManager.Value.Start(); }).Start();
        new Thread(() => { updateManager.Value.Start(); }).Start();

        // start setting last
        settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;
        settingsManager.Value.Start();

        // update Position and Size
        Height = (int)Math.Max(MinHeight, settingsManager.Value.GetDouble("MainWindowHeight"));
        Width = (int)Math.Max(MinWidth, settingsManager.Value.GetDouble("MainWindowWidth"));
        Left = Math.Min(SystemParameters.PrimaryScreenWidth - MinWidth, settingsManager.Value.GetDouble("MainWindowLeft"));
        Top = Math.Min(SystemParameters.PrimaryScreenHeight - MinHeight, settingsManager.Value.GetDouble("MainWindowTop"));
        navView.IsPaneOpen = settingsManager.Value.GetBoolean("MainWindowIsPaneOpen");
        MainWindowStartup.Stop();
        LogManager.LogInformation($"MainWindow Startup time Elapsed: {MainWindowStartup.ElapsedMilliseconds}ms");
        var example = new OverlayUtil(performanceManager,processManager,toastManager);
        new Thread(() => { example.Button_On(); }).Start();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // windows shutting down event
        if (msg == WM_QUERYENDSESSION)
        {
            // do something
        }

        return IntPtr.Zero;
    }



    private void ControllerManager_ControllerSelected(IController Controller)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            GamepadUISelectIcon.Glyph = Controller.GetGlyph(ButtonFlags.B1);
            GamepadUISelectIcon.Foreground = Controller.GetGlyphColor(ButtonFlags.B1);

            GamepadUIBackIcon.Glyph = Controller.GetGlyph(ButtonFlags.B2);
            GamepadUIBackIcon.Foreground = Controller.GetGlyphColor(ButtonFlags.B2);

            GamepadUIToggleIcon.Glyph = Controller.GetGlyph(ButtonFlags.B4);
            GamepadUIToggleIcon.Foreground = Controller.GetGlyphColor(ButtonFlags.B4);
        });
    }

    private void GamepadFocusManagerOnFocused(Control control)
    {
        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // todo : localize me
            string controlType = control.GetType().Name;
            switch (controlType)
            {
                default:
                    {
                        GamepadUISelect.Visibility = Visibility.Visible;
                        GamepadUIBack.Visibility = Visibility.Visible;
                        GamepadUIToggle.Visibility = Visibility.Collapsed;

                        GamepadUISelectDesc.Text = Properties.Resources.MainWindow_Select;
                        GamepadUIBackDesc.Text = Properties.Resources.MainWindow_Back;
                    }
                    break;

                case "Button":
                    {
                        GamepadUISelect.Visibility = Visibility.Visible;
                        GamepadUIBack.Visibility = Visibility.Visible;

                        GamepadUISelectDesc.Text = Properties.Resources.MainWindow_Select;
                        GamepadUIBackDesc.Text = Properties.Resources.MainWindow_Back;

                        // To get the first RadioButton in the list, if any
                        RadioButton firstRadioButton = WPFUtils.FindChildren(control).FirstOrDefault(c => c is RadioButton) as RadioButton;
                        if (firstRadioButton is not null)
                        {
                            GamepadUIToggle.Visibility = Visibility.Visible;
                            GamepadUIToggleDesc.Text = Properties.Resources.MainWindow_Toggle;
                        }
                    }
                    break;

                case "Slider":
                    {
                        GamepadUISelect.Visibility = Visibility.Collapsed;
                        GamepadUIBack.Visibility = Visibility.Visible;
                        GamepadUIToggle.Visibility = Visibility.Collapsed;
                    }
                    break;

                case "NavigationViewItem":
                    {
                        GamepadUISelect.Visibility = Visibility.Visible;
                        GamepadUIBack.Visibility = Visibility.Collapsed;
                        GamepadUIToggle.Visibility = Visibility.Collapsed;

                        GamepadUISelectDesc.Text = Properties.Resources.MainWindow_Navigate;
                    }
                    break;
            }
        });
    }

    private void AddNotifyIconItem(string name, object tag = null)
    {
        tag ??= string.Concat(name.Where(c => !char.IsWhiteSpace(c)));

        var menuItemMainWindow = new ToolStripMenuItem(name);
        menuItemMainWindow.Tag = tag;
        menuItemMainWindow.Click += MenuItem_Click;
        notifyIcon.ContextMenuStrip.Items.Add(menuItemMainWindow);
    }

    private void AddNotifyIconSeparator()
    {
        var separator = new ToolStripSeparator();
        notifyIcon.ContextMenuStrip.Items.Add(separator);
    }

    private void SettingsManager_SettingValueChanged(string name, object value)
    {
        switch (name)
        {
            case "ToastEnable":
                toastManager.Value.IsEnabled = Convert.ToBoolean(value);
                break;
            case "DesktopProfileOnStart":
                if (settingsManager.Value.IsInitialized)
                    break;

                var DesktopLayout = Convert.ToBoolean(value);
                settingsManager.Value.SetProperty("DesktopLayoutEnabled", DesktopLayout, false, true);
                break;
        }
    }

    public void SwapWindowState()
    {
        // UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    WindowState = WindowState.Minimized;
                    break;
                case WindowState.Minimized:
                    WindowState = prevWindowState;
                    break;
            }
        });
    }

    public static MainWindow GetCurrent()
    {
        return CurrentWindow;
    }

    private void loadPages()
    {
        //1. controller page
        controllerPage.Value.SetTag("controller");
        controllerPage.Value.ControllerPageLoaded += ControllerPage_Loaded;
        controllerPage.Value.Init();
        _pages.Add("ControllerPage", (Page)controllerPage.Value);

        //2. device page
        devicePage.Value.SetTag("device");
        devicePage.Value.Init();
        _pages.Add("DevicePage", (Page)devicePage.Value);

        //3. performance page
        performancePage.Value.SetTag("performance");
        performancePage.Value.Init();
        _pages.Add("PerformancePage", (Page)performancePage.Value);

        //4. profiles page
        profilesPage.Value.SetTag("profiles");
        profilesPage.Value.Init();
        _pages.Add("ProfilesPage", (Page)profilesPage.Value);

        //5. settings page
        settingsPage.Value.SetTag("settings");
        settingsPage.Value.Init();
        _pages.Add("SettingsPage", (Page)settingsPage.Value);

        //6. about page
        aboutPage.Value.SetTag("about");
        aboutPage.Value.Init();
        _pages.Add("AboutPage", (Page)aboutPage.Value);

        //7. overlay page
        overlayPage.Value.SetTag("overlay");
        overlayPage.Value.Init();
        _pages.Add("OverlayPage", (Page)overlayPage.Value);

        //8. hotkeys page
        hotkeysPage.Value.SetTag("hotkeys");
        hotkeysPage.Value.Init();
        _pages.Add("HotkeysPage", (Page)hotkeysPage.Value);

        //9. layout page
        layoutPage.Value.SetTag("layout");
        layoutPage.Value.SetParentNavView(navView);
        layoutPage.Value.Init();
        _pages.Add("LayoutPage", (Page)layoutPage.Value);

        //10. notifications page
        notificationsPage.Value.SetTag("notifications");
        notificationsPage.Value.Init();
        notificationsPage.Value.StatusChanged += NotificationsPage_LayoutUpdated;
        _pages.Add("NotificationsPage", (Page)notificationsPage.Value);
    }

    private void loadWindows()
    {
        // initialize overlay
        overlayModel.Value.Init();

        overlayTrackpad.Value.Init();

        overlayQuickTools.Value.Init();
    }

    private void GenericDeviceUpdated(PnPDevice device, DeviceEventArgs obj)
    {
        // todo: improve me
        CurrentDevice.PullSensors();

        aboutPage.Value.UpdateDevice(device);
        settingsPage.Value.UpdateDevice(device);
    }

    private void InputsManager_TriggerRaised(string listener, InputsChord input, InputsHotkeyType type, bool IsKeyDown,
        bool IsKeyUp)
    {
        switch (listener)
        {
            case "quickTools":
                overlayQuickTools.Value.ToggleVisibility();
                break;
            case "overlayGamepad":
                overlayModel.Value.ToggleVisibility();
                break;
            case "overlayTrackpads":
                overlayTrackpad.Value.ToggleVisibility();
                break;
            case "shortcutMainwindow":
                SwapWindowState();
                break;
        }
    }

    private void MenuItem_Click(object? sender, EventArgs e)
    {
        switch (((ToolStripMenuItem)sender).Tag)
        {
            case "MainWindow":
                SwapWindowState();
                break;
            case "QuickTools":
                overlayQuickTools.Value.ToggleVisibility();
                break;
            case "Exit":
                appClosing = true;
                Close();
                break;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // load gamepad navigation maanger
        gamepadFocusManager = new(this, ContentFrame, 
            settingsManager, 
            controllerManager, 
            uISounds,
            inputsManager);

        HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
        source.AddHook(WndProc); // Hook into the window's message loop
    }

    public void ControllerPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (IsReady)
            return;

        // hide splashscreen
        if (splashScreen is not null)
            splashScreen.Close();

        // home page has loaded, display main window
        WindowState = settingsManager.Value.GetBoolean("StartMinimized")
            ? WindowState.Minimized
            : (WindowState)settingsManager.Value.GetInt("MainWindowState");
        prevWindowState = (WindowState)settingsManager.Value.GetInt("MainWindowPrevState");

        IsReady = true;
    }

    private void NotificationsPage_LayoutUpdated(int status)
    {
        bool hasNotification = Convert.ToBoolean(status);

        // UI thread (async)
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            HasNotifications.Visibility = hasNotification ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private void VirtualManager_ControllerSelected(HIDmode HIDmode)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            overlayModel.Value.UpdateHIDMode(HIDmode);
        });
        CurrentDevice.SetKeyPressDelay(HIDmode);
    }

    public void UpdateSettings(Dictionary<string, string> args)
    {
        foreach (var pair in args)
        {
            var name = pair.Key;
            var property = pair.Value;

            switch (name)
            {
                case "DSUEnabled":
                    break;
                case "DSUip":
                    break;
                case "DSUport":
                    break;
            }
        }
    }

    // no code from the cases inside this function will be called on program start
    private async void OnSystemStatusChanged(SystemManager.SystemStatus status, SystemManager.SystemStatus prevStatus)
    {
        if (status == prevStatus)
            return;

        switch (status)
        {
            case SystemManager.SystemStatus.SystemReady:
                {
                    // resume from sleep
                    if (prevStatus == SystemManager.SystemStatus.SystemPending)
                    {
                        // use device-specific delay
                        await Task.Delay(CurrentDevice.ResumeDelay);

                        // restore inputs manager
                        inputsManager.Value.Start();

                        // start timer manager
                        timerManager.Value.Start();

                        // resume the virtual controller last
                        virtualManager.Value.Resume();

                        // restart IMU
                        sensorsManager.Value.Resume(true);
                    }

                    // open device, when ready
                    new Thread(() =>
                    {
                        // wait for all HIDs to be ready
                        while (!CurrentDevice.IsReady())
                            Thread.Sleep(500);

                        // open current device (threaded to avoid device to hang)
                        CurrentDevice.Open();
                    }).Start();
                }
                break;

            case SystemManager.SystemStatus.SystemPending:
                // sleep
                {
                    // stop the virtual controller
                    virtualManager.Value.Suspend();

                    // stop timer manager
                    timerManager.Value.Stop();

                    // stop sensors
                    sensorsManager.Value.Stop();

                    // pause inputs manager
                    inputsManager.Value.Stop();

                    // close current device
                    CurrentDevice.Close();
                }
                break;
        }
    }

    #region UI

    private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not null)
        {
            var navItem = (NavigationViewItem)args.InvokedItemContainer;
            var navItemTag = (string)navItem.Tag;

            switch (navItemTag)
            {
                default:
                    preNavItemTag = navItemTag;
                    break;
            }

            NavView_Navigate(preNavItemTag);
        }
    }

    public void NavView_Navigate(string navItemTag)
    {
        var item = _pages.FirstOrDefault(p => p.Key.Equals(navItemTag));
        var _page = item.Value;

        // Get the page type before navigation so you can prevent duplicate
        // entries in the backstack.
        var preNavPageType = ContentFrame.CurrentSourcePageType;

        // Only navigate if the selected page isn't currently loaded.
        if (!(_page is null) && !Equals(preNavPageType, _page)) NavView_Navigate(_page);
    }

    public static void NavView_Navigate(Page _page)
    {
        CurrentWindow.ContentFrame.Navigate(_page);
        CurrentWindow.scrollViewer.ScrollToTop();
    }

    private void navView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        TryGoBack();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        CurrentDevice.Close();

        notifyIcon.Visible = false;
        notifyIcon.Dispose();

        overlayModel.Value.Close();
        overlayTrackpad.Value.Close();
        overlayQuickTools.Value.Close(true);

        virtualManager.Value.Stop();
        multimediaManager.Value.Stop();
        gPUManager.Value.Stop();
        motionManager.Value.Stop();
        sensorsManager.Value.Stop();
        controllerManager.Value.Stop();
        inputsManager.Value.Stop();
        deviceManager.Value.Stop();
        platformManager.Value.Stop();
        oSDManager.Value.Stop();
        powerProfileManager.Value.Stop();
        profileManager.Value.Stop();
        layoutManager.Value.Stop();
        systemManager.Value.Stop();
        processManager.Value.Stop();
        toastManager.Value.Stop();
        taskManager.Value.Stop();
        performanceManager.Value.Stop();
        updateManager.Value.Stop();

        // closing page(s)
        controllerPage.Value.Page_Closed();
        profilesPage.Value.Page_Closed();
        settingsPage.Value.Page_Closed();
        overlayPage.Value.Page_Closed();
        hotkeysPage.Value.Page_Closed();
        layoutPage.Value.Page_Closed();
        notificationsPage.Value.Page_Closed();

        // force kill application
        Environment.Exit(0);
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        // position and size settings
        switch (WindowState)
        {
            case WindowState.Normal:
                settingsManager.Value.SetProperty("MainWindowLeft", Left);
                settingsManager.Value.SetProperty("MainWindowTop", Top);
                settingsManager.Value.SetProperty("MainWindowWidth", ActualWidth);
                settingsManager.Value.SetProperty("MainWindowHeight", ActualHeight);
                break;
            case WindowState.Maximized:
                settingsManager.Value.SetProperty("MainWindowLeft", 0);
                settingsManager.Value.SetProperty("MainWindowTop", 0);
                settingsManager.Value.SetProperty("MainWindowWidth", SystemParameters.MaximizedPrimaryScreenWidth);
                settingsManager.Value.SetProperty("MainWindowHeight", SystemParameters.MaximizedPrimaryScreenHeight);

                break;
        }

        settingsManager.Value.SetProperty("MainWindowState", (int)WindowState);
        settingsManager.Value.SetProperty("MainWindowPrevState", (int)prevWindowState);

        settingsManager.Value.SetProperty("MainWindowIsPaneOpen", navView.IsPaneOpen);

        if (settingsManager.Value.GetBoolean("CloseMinimises") && !appClosing)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            return;
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Minimized:
                notifyIcon.Visible = true;
                ShowInTaskbar = false;

                if (!NotifyInTaskbar)
                {
                    toastManager.Value.SendToast(Title, "is running in the background");
                    NotifyInTaskbar = true;
                }

                break;
            case WindowState.Normal:
            case WindowState.Maximized:
                notifyIcon.Visible = false;
                ShowInTaskbar = true;

                Activate();
                Topmost = true;  // important
                Topmost = false; // important
                Focus();

                prevWindowState = WindowState;
                break;
        }
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
        preNavItemTag = "ControllerPage";
        NavView_Navigate(preNavItemTag);
    }

    private void GamepadWindow_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!e.NewFocus.GetType().IsSubclassOf(typeof(Control)))
            return;

        GamepadFocusManagerOnFocused((Control)e.NewFocus);
    }

    private void GamepadWindow_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // do something
    }

    private bool TryGoBack()
    {
        if (!ContentFrame.CanGoBack)
            return false;

        // Don't go back if the nav pane is overlayed.
        if (navView.IsPaneOpen &&
            (navView.DisplayMode == NavigationViewDisplayMode.Compact ||
             navView.DisplayMode == NavigationViewDisplayMode.Minimal))
            return false;

        ContentFrame.GoBack();
        return true;
    }

    private void On_Navigated(object sender, NavigationEventArgs e)
    {
        navView.IsBackEnabled = ContentFrame.CanGoBack;

        if (ContentFrame.SourcePageType is not null)
        {
            CurrentPageName = ContentFrame.CurrentSourcePageType.Name;

            var NavViewItem = navView.MenuItems
                .OfType<NavigationViewItem>()
                .Where(n => n.Tag.Equals(CurrentPageName)).FirstOrDefault();

            if (!(NavViewItem is null))
                navView.SelectedItem = NavViewItem;

            navView.Header = new TextBlock() { Text = (string)((Page)e.Content).Title };
        }
    }

    #endregion
}