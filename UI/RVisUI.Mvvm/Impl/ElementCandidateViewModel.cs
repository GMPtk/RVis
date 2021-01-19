using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using static System.Globalization.CultureInfo;

namespace RVisUI.Mvvm
{
  public class ElementCandidateViewModel : ReactiveObject, IElementCandidateViewModel
  {
    public ElementCandidateViewModel(
      bool isUsed, 
      string name,
      string valueName,
      bool isIndependentVarable,
      IReadOnlyList<double> values,
      string? unit,
      string? description,
      ICommand changeDescriptionUnit
      )
    {
      _isUsed = isUsed;
      _name = name;
      _valueName = valueName;
      _isIndependentVariable = isIndependentVarable;
      _values = string.Join(", ", values.Take(5).Select(v => v.ToString("G4", InvariantCulture)));
      _unit = unit;
      _description = description;
      _changeUnitDescription = changeDescriptionUnit;
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

    public string ValueName
    {
      get => _valueName;
      set => this.RaiseAndSetIfChanged(ref _valueName, value);
    }
    private string _valueName;

    public bool IsIndependentVariable
    {
      get => _isIndependentVariable;
      set => this.RaiseAndSetIfChanged(ref _isIndependentVariable, value);
    }
    private bool _isIndependentVariable;

    public string Values
    {
      get => _values;
      set => this.RaiseAndSetIfChanged(ref _values, value);
    }
    private string _values;

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
