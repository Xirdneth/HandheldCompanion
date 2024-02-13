using HandheldCompanion.Managers;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Utils;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.Pages;

/// <summary>
///     Interaction logic for HotkeysPage.xaml
/// </summary>
public partial class HotkeysPage : Page, IHotkeysPage
{
    private readonly Lazy<IHotkeysManager> hotkeysManager;

    public HotkeysPage(Lazy<IHotkeysManager> hotkeysManager)
    {
        this.hotkeysManager = hotkeysManager;
        InitializeComponent();
    }

    public void SetTag(string Tag)
    {
        this.Tag = Tag;
    }

    public void Init()
    {
        hotkeysManager.Value.HotkeyCreated += HotkeysManager_HotkeyCreated;
        hotkeysManager.Value.HotkeyTypeCreated += HotkeysManager_HotkeyTypeCreated;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
    }

    public void Page_Closed()
    {
    }

    private void HotkeysManager_HotkeyTypeCreated(InputsHotkey.InputsHotkeyType type)
    {
        // These are special shortcut keys with no related events
        if (type == InputsHotkey.InputsHotkeyType.Embedded)
            return;

        // UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            SimpleStackPanel stackPanel = new()
            {
                Tag = type,
                Spacing = 6
            };

            var textBlock = new TextBlock { Text = EnumUtils.GetDescriptionFromEnumValue(type) };
            textBlock.SetResourceReference(StyleProperty, "BaseTextBlockStyle");

            stackPanel.Children.Add(textBlock);
            HotkeysPanel.Children.Add(stackPanel);
        });
    }

    private void HotkeysManager_HotkeyCreated(Hotkey hotkey)
    {
        // These are special shortcut keys with no related events
        if (hotkey.inputsHotkey.hotkeyType == InputsHotkey.InputsHotkeyType.Embedded)
            return;

        var DeviceType = hotkey.inputsHotkey.DeviceType;
        if (DeviceType is not null && DeviceType != MainWindow.CurrentDevice.GetType())
            return;

        // UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            var control = hotkey.GetControl();

            var idx = (ushort)hotkey.inputsHotkey.hotkeyType;

            var stackPanel = (SimpleStackPanel)HotkeysPanel.Children[idx];
            stackPanel.Children.Add(control);
        });
    }
}