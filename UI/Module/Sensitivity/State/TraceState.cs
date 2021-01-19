using OxyPlot;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using static System.Double;

namespace Sensitivity
{
  internal sealed class TraceState : INotifyPropertyChanged
  {
    internal double? ViewHeight
    {
      get => _viewHeight;
      set => this.RaiseAndSetIfChanged(ref _viewHeight, value, PropertyChanged);
    }
    private double? _viewHeight;

    public string? ChartTitle
    {
      get => _chartTitle;
      set => this.RaiseAndSetIfChanged(ref _chartTitle, value, PropertyChanged);
    }
    private string? _chartTitle;

    public string? YAxisTitle
    {
      get => _yAxisTitle;
      set => this.RaiseAndSetIfChanged(ref _yAxisTitle, value, PropertyChanged);
    }
    private string? _yAxisTitle;

    public string? XAxisTitle
    {
      get => _xAxisTitle;
      set => this.RaiseAndSetIfChanged(ref _xAxisTitle, value, PropertyChanged);
    }
    private string? _xAxisTitle;

    internal OxyColor? MarkerFill
    {
      get => _markerFill;
      set => this.RaiseAndSetIfChanged(ref _markerFill, value, PropertyChanged);
    }
    private OxyColor? _markerFill;

    internal OxyColor? SeriesColor
    {
      get => _seriesColor;
      set => this.RaiseAndSetIfChanged(ref _seriesColor, value, PropertyChanged);
    }
    private OxyColor? _seriesColor;

    public double HorizontalAxisMinimum
    {
      get => _horizontalAxisMinimum;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisMinimum, value, PropertyChanged);
    }
    private double _horizontalAxisMinimum = NaN;

    public double HorizontalAxisMaximum
    {
      get => _horizontalAxisMaximum;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisMaximum, value, PropertyChanged);
    }
    private double _horizontalAxisMaximum = NaN;

    public double HorizontalAxisAbsoluteMinimum
    {
      get => _horizontalAxisAbsoluteMinimum;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisAbsoluteMinimum, value, PropertyChanged);
    }
    private double _horizontalAxisAbsoluteMinimum = MinValue;

    public double HorizontalAxisAbsoluteMaximum
    {
      get => _horizontalAxisAbsoluteMaximum;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisAbsoluteMaximum, value, PropertyChanged);
    }
    private double _horizontalAxisAbsoluteMaximum = MaxValue;

    public double VerticalAxisMinimum
    {
      get => _verticalAxisMinimum;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisMinimum, value, PropertyChanged);
    }
    private double _verticalAxisMinimum = NaN;

    public double VerticalAxisMaximum
    {
      get => _verticalAxisMaximum;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisMaximum, value, PropertyChanged);
    }
    private double _verticalAxisMaximum = NaN;

    public double VerticalAxisAbsoluteMinimum
    {
      get => _verticalAxisAbsoluteMinimum;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisAbsoluteMinimum, value, PropertyChanged);
    }
    private double _verticalAxisAbsoluteMinimum = MinValue;

    public double VerticalAxisAbsoluteMaximum
    {
      get => _verticalAxisAbsoluteMaximum;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisAbsoluteMaximum, value, PropertyChanged);
    }
    private double _verticalAxisAbsoluteMaximum = MaxValue;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
