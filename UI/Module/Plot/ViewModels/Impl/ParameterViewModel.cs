using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using static RVis.Base.Check;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.Math;
using static System.String;

namespace Plot
{
  public class ParameterViewModel : IParameterViewModel, INotifyPropertyChanged
  {
    internal ParameterViewModel(
      ICommand toggleSelect,
      ICommand resetValue,
      ICommand increaseMinimum,
      ICommand decreaseMinimum,
      ICommand increaseMaximum,
      ICommand decreaseMaximum,
      string name,
      string defaultValue,
      string? unit,
      string? description,
      IAppService appService
      )
    {
      ToggleSelect = toggleSelect;
      ResetValue = resetValue;
      IncreaseMinimum = increaseMinimum;
      DecreaseMinimum = decreaseMinimum;
      IncreaseMaximum = increaseMaximum;
      DecreaseMaximum = decreaseMaximum;
      DefaultValue = defaultValue;
      Unit = unit;
      Description = description;

      Name = name;
      SortKey = Name.ToUpperInvariant();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      this.ObservableForProperty(vm => vm.TValue).Subscribe(
        _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveTValue)
        );

      this.ObservableForProperty(vm => vm.NValue).Subscribe(
        _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveNValue)
        );

      this.ObservableForProperty(vm => vm.Minimum).Subscribe(
        _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveMinimum)
        );

      this.ObservableForProperty(vm => vm.Maximum).Subscribe(
        _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveMaximum)
        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        EnsureValidValueBounds();
        UpdateEnable();
        ConfigureTicks();
      }
    }

    public string Name { get; }
    public string SortKey { get; }
    public string DefaultValue { get; }
    public string? Unit { get; }
    public string? Description { get; }

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public ICommand ToggleSelect { get; }

    public string? TValue
    {
      get => _tValue;
      set => this.RaiseAndSetIfChanged(ref _tValue, value, PropertyChanged);
    }
    private string? _tValue;

    public double? NValue
    {
      get => _nValue;
      set => this.RaiseAndSetIfChanged(ref _nValue, value, PropertyChanged);
    }
    private double? _nValue;

    public ICommand ResetValue { get; }

    public double Minimum
    {
      get => _minimum;
      set => this.RaiseAndSetIfChanged(ref _minimum, value, PropertyChanged);
    }
    private double _minimum;

    public ICommand IncreaseMinimum { get; }

    public bool CanIncreaseMinimum
    {
      get => _canIncreaseMinimum;
      set => this.RaiseAndSetIfChanged(ref _canIncreaseMinimum, value, PropertyChanged);
    }
    private bool _canIncreaseMinimum;

    public ICommand DecreaseMinimum { get; }

    public double Maximum
    {
      get => _maximum;
      set => this.RaiseAndSetIfChanged(ref _maximum, value, PropertyChanged);
    }
    private double _maximum;

    public ICommand IncreaseMaximum { get; }

    public ICommand DecreaseMaximum { get; }

    public bool CanDecreaseMaximum
    {
      get => _canDecreaseMaximum;
      set => this.RaiseAndSetIfChanged(ref _canDecreaseMaximum, value, PropertyChanged);
    }
    private bool _canDecreaseMaximum;

    public string? Ticks
    {
      get => _ticks;
      set => this.RaiseAndSetIfChanged(ref _ticks, value, PropertyChanged);
    }
    private string? _ticks;

    public void Set(double value, double minimum, double maximum)
    {
      RequireTrue(value > minimum && value < maximum);

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        NValue = value;
        TValue = value.ToString(InvariantCulture);
        Minimum = minimum;
        Maximum = maximum;

        UpdateEnable();

        ConfigureTicks();
      }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ConfigureTicks()
    {
      // start with range in divided in 10
      var range = Maximum - Minimum;
      var division = range / 10.0;

      // make the division some round number
      var roundedDown = Pow(10, Floor(Log10(division)));
      var fromTen = 10.0 - division / roundedDown;
      division = fromTen < 2.5
        ? roundedDown * 10.0 : fromTen < 5.0
        ? roundedDown * 5.0
        : roundedDown;

      // make a set of ticks by counting down in division units 
      // from some round number above the maximum
      var tick = Sign(Maximum) * Pow(10, Sign(Maximum) + Floor(Log10(Abs(Maximum))));
      var ticks = new List<double>();
      while (tick > Minimum)
      {
        if (tick < Maximum) ticks.Add(tick);
        tick -= division;
      }

      // include bounds
      ticks.Add(Minimum);
      ticks.Reverse();
      ticks.Add(Maximum);

      Ticks = Join(",", ticks);
    }

    private void ObserveTValue(object _)
    {
      NValue = TryParse(_tValue, out double nValue) ? nValue : default(double?);

      UpdateEnable();

      if (EnsureValidValueBounds())
      {
        ConfigureTicks();
      }
    }

    private void ObserveNValue(object _)
    {
      TValue = _nValue.HasValue ? _nValue.Value.ToString(InvariantCulture) : Empty;

      UpdateEnable();

      if (EnsureValidValueBounds())
      {
        ConfigureTicks();
      }
    }

    private void ObserveMinimum(object _)
    {
      UpdateEnable();
      ConfigureTicks();
    }

    private void ObserveMaximum(object _)
    {
      UpdateEnable();
      ConfigureTicks();
    }

    private bool EnsureValidValueBounds()
    {
      bool didUpdate = false;

      if (_nValue.HasValue)
      {
        if (Minimum > _nValue)
        {
          Minimum = _nValue.Value.GetPreviousOrderOfMagnitude();
          didUpdate = true;
        }
        if (Maximum < _nValue)
        {
          Maximum = _nValue.Value.GetNextOrderOfMagnitude();
          didUpdate = true;
        }
      }

      return didUpdate;
    }

    private void UpdateEnable()
    {
      if (NValue.HasValue)
      {
        var minNextOOM = Minimum.GetNextOrderOfMagnitude();
        CanIncreaseMinimum = minNextOOM < NValue.Value;

        var maxPreviousOOM = Maximum.GetPreviousOrderOfMagnitude();
        CanDecreaseMaximum = maxPreviousOOM > NValue.Value;
      }
      else
      {
        CanIncreaseMinimum = false;
        CanDecreaseMaximum = false;
      }
    }

    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
  }
}
