using LanguageExt;
using OxyPlot;
using ReactiveUI;
using RVis.Model;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVisUI.Wpf.WpfTools;
using static System.Double;

namespace RVisUI.AppInf
{
  [DisplayName("Invariant")]
  public sealed class InvariantDistributionViewModel : DistributionViewModelBase, IInvariantDistributionViewModel, INotifyPropertyChanged
  {
    public InvariantDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
    {
      this
        .WhenAny(vm => vm.Distribution, _ => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistribution
            )
          );

      this
        .WhenAny(vm => vm.Value, _ => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistributionParameters
            )
          );
    }

    public InvariantDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.Invariant;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<InvariantDistribution> Distribution
    {
      get => _invariantDistribution;
      set => this.RaiseAndSetIfChanged(ref _invariantDistribution, value, PropertyChanged);
    }
    private Option<InvariantDistribution> _invariantDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _invariantDistribution.Match(lnd => lnd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<InvariantDistribution>(value));
    }

    public string? Variable
    {
      get => _variable;
      set => this.RaiseAndSetIfChanged(ref _variable, value, PropertyChanged);
    }
    private string? _variable;

    public string? Unit
    {
      get => _unit;
      set => this.RaiseAndSetIfChanged(ref _unit, value, PropertyChanged);
    }
    private string? _unit;

    public double? Value
    {
      get => _value;
      set => this.RaiseAndSetIfChanged(ref _value, value, PropertyChanged);
    }
    private double? _value;

    public PlotModel PlotModel => throw new InvalidOperationException("Trying to get plot for invariant");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ObserveDistribution(object? _)
    {
      Distribution.Match(
        invariant =>
        {
          Value = IsNaN(invariant.Value) ? default(double?) : invariant.Value;
        },
        () =>
        {
          Value = default;
        });
    }

    private void ObserveDistributionParameters(object? _)
    {
      if (Value.HasValue)
      {
        Distribution = new InvariantDistribution(Value.Value);
      }
    }
  }
}
