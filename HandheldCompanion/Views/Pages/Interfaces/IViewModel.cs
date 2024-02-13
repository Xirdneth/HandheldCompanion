using LiveCharts;
using LiveCharts.Events;
using LiveCharts.Wpf;
using System;
using System.ComponentModel;

namespace HandheldCompanion.Views.Pages
{
    public interface IViewModel
    {
        MyCommand<ChartPoint> DataClickCommand { get; set; }
        MyCommand<ChartPoint> DataHoverCommand { get; set; }
        Func<double, string> Formatter { get; set; }
        MyCommand<RangeChangedEventArgs> RangeChangedCommand { get; set; }
        MyCommand<CartesianChart> UpdaterTickCommand { get; set; }
        double XPointer { get; set; }
        double YPointer { get; set; }

        event PropertyChangedEventHandler PropertyChanged;
    }
}