using HandheldCompanion.Controllers;

using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for GyroPage.xaml
    /// </summary>
    public partial class GyroPage : ILayoutPage
    {
        public static List<AxisLayoutFlags> Gyroscope = new() { AxisLayoutFlags.Gyroscope };
        private readonly Lazy<IHotkeysManager> hotkeysManager;
        private readonly Lazy<IControllerManager> controllerManager;
        private readonly Lazy<ITimerManager> timerManager;

        public GyroPage(
            Lazy<IHotkeysManager> hotkeysManager,
            Lazy<IControllerManager> controllerManager, 
            Lazy<IInputsManager> inputsManager,
            Lazy<ITimerManager> timerManager)
        {
            InitializeComponent();
            this.hotkeysManager = hotkeysManager;
            this.controllerManager = controllerManager;
            this.timerManager = timerManager;
            // draw UI
            foreach (AxisLayoutFlags axis in Gyroscope)
            {
                GyroMapping gyroMapping = new GyroMapping(axis,hotkeysManager,controllerManager,inputsManager,timerManager);
                GyroscopePanel.Children.Add(gyroMapping);

                GyroMappings.Add(axis, gyroMapping);
            }
        }

        public override void UpdateController(IController controller)
        {
            base.UpdateController(controller);

            bool gyro = CheckController(controller, Gyroscope) || MainWindow.CurrentDevice.HasMotionSensor();

            gridGyroscope.Visibility = gyro ? Visibility.Visible : Visibility.Collapsed;

            enabled = gyro;
        }

        public GyroPage(string Tag,
            Lazy<IHotkeysManager> hotkeysManager, 
            Lazy<IControllerManager> controllerManager,
            Lazy<IInputsManager> inputsManager,
            Lazy<ITimerManager> timerManager): this (hotkeysManager, controllerManager, inputsManager, timerManager)
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
}