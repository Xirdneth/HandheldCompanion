using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Simulators;
using System;
using System.ComponentModel;
using System.Numerics;

namespace HandheldCompanion.Actions
{
    [Serializable]
    public enum SpecialActionsType
    {
        [Description("Flick Stick")]
        FlickStick = 0,
    }

    [Serializable]
    public class SpecialActions : IActions
    {
        public SpecialActionsType SpecialType;

        // runtime variables
        private FlickStick flickStick { get; set; }
        private float remainder = 0;

        // settings
        public float FlickSensitivity = 5.0f;
        public float SweepSensitivity = 5.0f;
        public float FlickThreshold = 0.75f;
        public int FlickSpeed = 100;
        public int FlickFrontAngleDeadzone = 15;

        public SpecialActions(Lazy<IControllerManager> controllerManager, Lazy<ITimerManager> timerManager) : base(controllerManager, timerManager)
        {
            this.actionType = ActionType.Special;
            flickStick = new(timerManager);
        }

        public SpecialActions(SpecialActionsType type,
            Lazy<IControllerManager> controllerManager, Lazy<ITimerManager> timerManager):this(controllerManager, timerManager)
        {
            this.SpecialType = type;
        }

        public void Execute(AxisLayout layout)
        {
            if (layout.vector == Vector2.Zero)
                return;

            float delta = flickStick.Handle(layout.vector, FlickSensitivity, SweepSensitivity,
                                            FlickThreshold, FlickSpeed, FlickFrontAngleDeadzone);

            delta += remainder;
            int intDelta = (int)Math.Truncate(delta);
            remainder = delta - intDelta;

            MouseSimulator.MoveBy(intDelta, 0);
        }
    }
}
