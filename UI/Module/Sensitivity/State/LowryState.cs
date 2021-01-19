using OxyPlot;
using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sensitivity
{
  internal sealed class LowryState : INotifyPropertyChanged
  {
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

    internal OxyColor? InteractionsFillColor
    {
      get => _interactionsFillColor;
      set => this.RaiseAndSetIfChanged(ref _interactionsFillColor, value, PropertyChanged);
    }
    private OxyColor? _interactionsFillColor;

    internal OxyColor? MainEffectsFillColor
    {
      get => _mainEffectsFillColor;
      set => this.RaiseAndSetIfChanged(ref _mainEffectsFillColor, value, PropertyChanged);
    }
    private OxyColor? _mainEffectsFillColor;

    internal OxyColor? SmokeFill
    {
      get => _smokeFill;
      set => this.RaiseAndSetIfChanged(ref _smokeFill, value, PropertyChanged);
    }
    private OxyColor? _smokeFill;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
