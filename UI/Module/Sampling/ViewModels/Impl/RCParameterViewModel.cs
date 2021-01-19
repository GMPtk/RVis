using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Windows.Input;
using static System.Globalization.CultureInfo;

namespace Sampling
{
  internal sealed class RCParameterViewModel : IRCParameterViewModel, INotifyPropertyChanged
  {
    internal RCParameterViewModel(string name, ICommand? setKeyboardTarget)
    {
      Name = name;
      SetKeyboardTarget = setKeyboardTarget;

      _reactiveSafeInvoke = new ReactiveSafeInvoke();

      this
        .ObservableForProperty(vm => vm.CorrelationN)
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveCorrelationN
            )
          );

      this
        .ObservableForProperty(vm => vm.CorrelationT)
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveCorrelationT
            )
          );
    }

    public string Name { get; }

    public ICommand? SetKeyboardTarget { get; }

    public double? CorrelationN
    {
      get => _correlationN;
      set => this.RaiseAndSetIfChanged(ref _correlationN, value, PropertyChanged);
    }
    private double? _correlationN;

    public string? CorrelationT
    {
      get => _correlationT;
      set => this.RaiseAndSetIfChanged(ref _correlationT, value.CheckParseValue<double>(), PropertyChanged);
    }
    private string? _correlationT;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ObserveCorrelationN(object obj)
    {
      CorrelationT = _correlationN?.ToString(InvariantCulture);
    }

    private void ObserveCorrelationT(object obj)
    {
      CorrelationN = double.TryParse(_correlationT, out double d)
        ? d
        : default(double?);
    }

    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
  }
}
