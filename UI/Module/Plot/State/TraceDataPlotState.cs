using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Plot
{
  public class TraceDataPlotState : INotifyPropertyChanged
  {
    public TraceDataPlotState()
    {
      IsAxesOriginLockedToZeroZero = true;
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value, PropertyChanged);
    }
    private bool _isVisible;

    public DepVarConfigState DepVarConfigState { get; } = new DepVarConfigState();

    public string? SelectedSeriesName
    {
      get => _selectedSeriesName;
      set => this.RaiseAndSetIfChanged(ref _selectedSeriesName, value, PropertyChanged);
    }
    private string? _selectedSeriesName;

    public double ViewHeight
    {
      get => _viewHeight;
      set => this.RaiseAndSetIfChanged(ref _viewHeight, value, PropertyChanged);
    }
    private double _viewHeight;

    public bool IsSeriesTypeLine
    {
      get => _isSeriesTypeLine;
      set => this.RaiseAndSetIfChanged(ref _isSeriesTypeLine, value, PropertyChanged);
    }
    private bool _isSeriesTypeLine;

    public bool IsAxesOriginLockedToZeroZero
    {
      get => _isAxesOriginLockedToZeroZero;
      set => this.RaiseAndSetIfChanged(ref _isAxesOriginLockedToZeroZero, value, PropertyChanged);
    }
    private bool _isAxesOriginLockedToZeroZero;

    public double? XMinimum
    {
      get => _xMinimum;
      set => this.RaiseAndSetIfChanged(ref _xMinimum, value, PropertyChanged);
    }
    private double? _xMinimum;

    public double? XMaximum
    {
      get => _xMaximum;
      set => this.RaiseAndSetIfChanged(ref _xMaximum, value, PropertyChanged);
    }
    private double? _xMaximum;

    public double? YMinimum
    {
      get => _yMinimum;
      set => this.RaiseAndSetIfChanged(ref _yMinimum, value, PropertyChanged);
    }
    private double? _yMinimum;

    public double? YMaximum
    {
      get => _yMaximum;
      set => this.RaiseAndSetIfChanged(ref _yMaximum, value, PropertyChanged);
    }
    private double? _yMaximum;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
