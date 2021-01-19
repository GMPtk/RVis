using RVis.Base;
using RVis.Base.Extensions;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using static System.Globalization.CultureInfo;

namespace Plot
{
  public class ParameterEditState : INotifyPropertyChanged, IDeepCloneable<ParameterEditState>
  {
    public ParameterEditState() { }

    public ParameterEditState(string name, double scalar)
    {
      _name = name;
      _value = scalar.ToString(InvariantCulture);
      _minimum = scalar.GetPreviousOrderOfMagnitude();
      _maximum = scalar.GetNextOrderOfMagnitude();
    }

    public ParameterEditState(ParameterEditState deepCopySource)
    {
      _name = deepCopySource.Name;
      _isSelected = deepCopySource.IsSelected;
      _value = deepCopySource.Value;
      _minimum = deepCopySource.Minimum;
      _maximum = deepCopySource.Maximum;
    }

    public string? Name
    {
      get => _name;
      set => this.RaiseAndSetIfChanged(ref _name, value, PropertyChanged);
    }
    private string? _name;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public string? Value
    {
      get => _value;
      set => this.RaiseAndSetIfChanged(ref _value, value, PropertyChanged);
    }
    private string? _value;

    public double Minimum
    {
      get => _minimum;
      set => this.RaiseAndSetIfChanged(ref _minimum, value, PropertyChanged);
    }
    private double _minimum;

    public double Maximum
    {
      get => _maximum;
      set => this.RaiseAndSetIfChanged(ref _maximum, value, PropertyChanged);
    }
    private double _maximum;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ParameterEditState DeepClone() => new ParameterEditState(this);

    object IDeepCloneable.DeepClone() => DeepClone();
  }
}
