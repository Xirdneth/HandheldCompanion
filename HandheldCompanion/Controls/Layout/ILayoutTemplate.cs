using HandheldCompanion.Managers;
using System;

namespace HandheldCompanion.Controls
{
    public interface ILayoutTemplate
    {
        string Author { get; set; }
        Type ControllerType { get; set; }
        string Description { get; set; }

        string Executable { get; set; }

        Guid Guid { get; set; }
        bool IsInternal { get; set; }

        Layout Layout { get; set; }
        string Name { get; set; }
        string Product { get; set; }

        event LayoutTemplate.UpdatedEventHandler Updated;

        void ClearDelegates();
        int CompareTo(object obj);
        void InitializeComponent();
    }
}