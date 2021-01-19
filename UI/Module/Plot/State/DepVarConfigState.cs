using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model.Extensions;
using System.Collections.Generic;
using System.ComponentModel;

namespace Plot
{
  public class DepVarConfigState : INotifyPropertyChanged
  {
    public DepVarConfigState()
    {
      InactiveSupplementaryElementNames = new SortedDictionary<string, Arr<string>>();
    }

    public string? SelectedElementName
    {
      get => _selectedElementName;
      set
      {
        if (_selectedElementName.IsAString())
        {
          if (InactiveSupplementaryElementNames.ContainsKey(_selectedElementName))
          {
            InactiveSupplementaryElementNames[_selectedElementName] = SupplementaryElementNames;
          }
          else
          {
            InactiveSupplementaryElementNames.Add(_selectedElementName, SupplementaryElementNames);
          }
        }

        _supplementaryElementNames = value.IsAString() && InactiveSupplementaryElementNames.ContainsKey(value)
          ? InactiveSupplementaryElementNames[value]
          : default;

        this.RaiseAndSetIfChanged(ref _selectedElementName, value, PropertyChanged);
      }
    }
    private string? _selectedElementName;

    public Arr<string> MRUElementNames
    {
      get => _mruElementNames;
      set => this.RaiseAndSetIfChanged(ref _mruElementNames, value, PropertyChanged);
    }
    private Arr<string> _mruElementNames;

    public string? SelectedInsetElementName
    {
      get => _selectedInsetElementName;
      set => this.RaiseAndSetIfChanged(ref _selectedInsetElementName, value, PropertyChanged);
    }
    private string? _selectedInsetElementName;

    public Arr<string> SupplementaryElementNames
    {
      get => _supplementaryElementNames;
      set => this.RaiseAndSetIfChanged(ref _supplementaryElementNames, value, PropertyChanged);
    }
    private Arr<string> _supplementaryElementNames;

    public IDictionary<string, Arr<string>> InactiveSupplementaryElementNames { get; }

    public Arr<string> ObservationsReferences
    {
      get => _observationsReferences;
      set => this.RaiseAndSetIfChanged(ref _observationsReferences, value, PropertyChanged);
    }
    private Arr<string> _observationsReferences;

    public bool IsScaleLogarithmic
    {
      get => _isScaleLogarithmic;
      set => this.RaiseAndSetIfChanged(ref _isScaleLogarithmic, value, PropertyChanged);
    }
    private bool _isScaleLogarithmic;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
