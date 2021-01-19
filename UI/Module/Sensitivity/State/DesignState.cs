using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sensitivity
{
  internal sealed class DesignState : INotifyPropertyChanged
  {
    public SensitivityMethod? SensitivityMethod
    {
      get => _sensitivityMethod;
      set => this.RaiseAndSetIfChanged(ref _sensitivityMethod, value, PropertyChanged);
    }
    private SensitivityMethod? _sensitivityMethod;

    public int? NoOfRuns
    {
      get => _noOfRuns;
      set => this.RaiseAndSetIfChanged(ref _noOfRuns, value, PropertyChanged);
    }
    private int? _noOfRuns;

    public int? NoOfSamples
    {
      get => _noOfSamples;
      set => this.RaiseAndSetIfChanged(ref _noOfSamples, value, PropertyChanged);
    }
    private int? _noOfSamples;

    internal string? SelectedElementName
    {
      get => _selectedElementName;
      set => this.RaiseAndSetIfChanged(ref _selectedElementName, value, PropertyChanged);
    }
    private string? _selectedElementName;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
