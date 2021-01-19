using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Windows.Input;

namespace Sampling
{
  internal sealed class OutputsFilterViewModel : IOutputsFilterViewModel, INotifyPropertyChanged
  {
    public OutputsFilterViewModel(
      string independentVariableName,
      double independentVariableValue,
      string? independentVariableUnit,
      string outputName,
      double from,
      double to,
      string? outputUnit,
      ICommand toggleEnable,
      ICommand delete
      )
    {
      IndependentVariableName = independentVariableName;
      IndependentVariableValue = independentVariableValue.ToString("G4");
      IndependentVariableUnit = independentVariableUnit;
      OutputName = outputName;
      From = from.ToString("G4");
      To = to.ToString("G4");
      OutputUnit = outputUnit;
      ToggleEnable = toggleEnable;
      Delete = delete;
    }

    public string IndependentVariableName { get; }
    public string IndependentVariableValue { get; }
    public string? IndependentVariableUnit { get; }

    public string OutputName { get; }
    public string From { get; }
    public string To { get; }
    public string? OutputUnit { get; }

    public bool IsEnabled
    {
      get => _isEnabled;
      set => this.RaiseAndSetIfChanged(ref _isEnabled, value, PropertyChanged);
    }
    private bool _isEnabled;

    public int FilterHashCode { get; set; }

    public ICommand ToggleEnable { get; }

    public ICommand Delete { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
