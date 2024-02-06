using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HandheldCompanion.ViewModels
{
    public class ViewModelBase : ObservableObject, IDisposable
    {
        public virtual void Dispose() { }
    }
}
