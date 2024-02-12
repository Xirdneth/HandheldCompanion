using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using System;
using System.Collections.Generic;
using System.Windows;

namespace HandheldCompanion.Views.Pages;

/// <summary>
///     Interaction logic for DpadPage.xaml
/// </summary>
public partial class DpadPage : ILayoutPage
{
    public static List<ButtonFlags> DPAD = new()
        { ButtonFlags.DPadUp, ButtonFlags.DPadDown, ButtonFlags.DPadLeft, ButtonFlags.DPadRight };
    private readonly Lazy<IControllerManager> controllerManager;
    private readonly Lazy<ITimerManager> timerManager;

    public DpadPage(Lazy<IControllerManager> controllerManager, Lazy<ITimerManager> timerManager)
    {
        InitializeComponent();
        this.controllerManager = controllerManager;
        this.timerManager = timerManager;
        // draw UI
        foreach (ButtonFlags button in DPAD)
        {
            ButtonStack panel = new(button, controllerManager, timerManager);
            DpadStackPanel.Children.Add(panel);

            ButtonStacks.Add(button, panel);
        }
    }

    public override void UpdateController(IController controller)
    {
        base.UpdateController(controller);

        bool dpad = CheckController(controller, DPAD);

        gridDpad.Visibility = dpad ? Visibility.Visible : Visibility.Collapsed;

        enabled = dpad;
    }

    public DpadPage(string Tag,
        Lazy<IControllerManager> controllerManager, Lazy<ITimerManager> timerManager) : this(controllerManager, timerManager)
    {
        this.Tag = Tag;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
    }

    public void Page_Closed()
    {
    }
}