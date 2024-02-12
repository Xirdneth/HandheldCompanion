using HandheldCompanion.Managers;
using SharpDX.DirectInput;
using System;

namespace HandheldCompanion.Controllers;

public class DInputController : IController
{
    public Joystick joystick;
    protected JoystickState State = new();

    public DInputController(
        Lazy<ISettingsManager> settingsManager, 
        Lazy<IControllerManager> controllerManager) :base(settingsManager, controllerManager)
    {
    }

    public DInputController(Joystick joystick, PnPDetails details,
        Lazy<ISettingsManager> settingsManager,
        Lazy<IControllerManager> controllerManager) : this(settingsManager, controllerManager)
    {
        if (joystick is null)
            return;

        this.joystick = joystick;
        UserIndex = (byte)joystick.Properties.JoystickId;

        if (details is null)
            return;

        Details = details;
        Details.isHooked = true;

        // Set BufferSize in order to use buffered data.
        joystick.Properties.BufferSize = 128;

        // UI
        DrawUI();
        UpdateUI();
    }

    public override string ToString()
    {
        var baseName = base.ToString();
        if (!string.IsNullOrEmpty(baseName))
            return baseName;
        if (!string.IsNullOrEmpty(joystick.Information.ProductName))
            return joystick.Information.ProductName;
        return $"DInput Controller {UserIndex}";
    }

    public override void UpdateInputs(long ticks)
    {
        base.UpdateInputs(ticks);
    }

    public override bool IsConnected()
    {
        return (bool)!joystick?.IsDisposed;
    }

    public override void Plug()
    {
        if (joystick is not null)
            joystick.Acquire();

        base.Plug();
    }

    public override void Unplug()
    {
        // Unacquire the joystick
        if (joystick is not null)
            joystick.Unacquire();

        base.Unplug();
    }
}