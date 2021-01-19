using ReactiveUI;
using System.Windows.Input;

namespace RVisUI.Mvvm
{
  public class ParameterCandidateViewModel : ReactiveObject, IParameterCandidateViewModel
  {
    public ParameterCandidateViewModel(
      bool isUsed, 
      string name, 
      double value,
      string? unit,
      string? description,
      ICommand changeUnitDescription
      )
    {
      _isUsed = isUsed;
      _name = name;
      _value = value;
      _unit = unit;
      _description = description;
      _changeUnitDescription = changeUnitDescription;
    }

    public bool IsUsed
    {
      get => _isUsed;
      set => this.RaiseAndSetIfChanged(ref _isUsed, value);
    }
    private bool _isUsed;

    public string Name
    {
      get => _name;
      set => this.RaiseAndSetIfChanged(ref _name, value);
    }
    private string _name;

    public double Value
    {
      get => _value;
      set => this.RaiseAndSetIfChanged(ref _value, value);
    }
    private double _value;

    public string? Unit
    {
      get => _unit;
      set => this.RaiseAndSetIfChanged(ref _unit, value);
    }
    private string? _unit;

    public string? Description
    {
      get => _description;
      set => this.RaiseAndSetIfChanged(ref _description, value);
    }
    private string? _description;

    public ICommand ChangeUnitDescription
    {
      get => _changeUnitDescription;
      set => this.RaiseAndSetIfChanged(ref _changeUnitDescription, value);
    }
    private ICommand _changeUnitDescription;
  }
}
