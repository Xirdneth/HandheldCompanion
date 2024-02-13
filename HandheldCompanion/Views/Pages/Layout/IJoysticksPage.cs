using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using System.Collections.Generic;

namespace HandheldCompanion.Views.Pages
{
    public interface IJoysticksPage
    {
        void Init();
        void InitializeComponent();
        void Page_Closed();
        void UpdateController(IController controller);
        public Dictionary<ButtonFlags, ButtonStack> ButtonStacks { get; set; }
        public Dictionary<AxisLayoutFlags, AxisMapping> AxisMappings { get; set; }
    }
}