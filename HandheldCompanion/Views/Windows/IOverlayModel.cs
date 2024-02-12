using HandheldCompanion.Controllers;
using HandheldCompanion.Utils;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace HandheldCompanion.Views.Windows
{
    public interface IOverlayModel
    {
        void InitializeComponent();
        void ToggleVisibility();
        void UpdateHIDMode(HIDmode HIDmode);
        void UpdateInterval(double interval);
        void UpdateModel();
        void UpdateOverlayMode(OverlayModelMode Modelmode);
        void UpdateReport(ControllerState Inputs);
        void Init();
        void Close();
        VerticalAlignment VerticalAlignment { get; set; }
        HorizontalAlignment HorizontalAlignment { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        bool MotionActivated { get; set; }
        bool FaceCamera { get; set; }
        public void SetDesiredAngleDegX(double value);
        public void SetDesiredAngleDegY(double value);
        void ModelViewPortSetValue(DependencyProperty dependencyProperty, EdgeMode edgeMode);
        void ModelViewPortSetOpacity(double value);
        Brush Background { get; set; }
        bool Topmost { get; set; }
        Visibility Visibility { get; set; }
    }
}