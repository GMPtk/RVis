using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVisUI.Wpf.WpfTools;
using static System.Double;

namespace RVisUI.AppInf
{
  [DisplayName("Beta Scaled")]
  public sealed class BetaScaledDistributionViewModel : DistributionViewModelBase, IBetaScaledDistributionViewModel, INotifyPropertyChanged
  {
    public BetaScaledDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
    {
      PlotModel = new PlotModel
      {
        IsLegendVisible = false
      };
      PlotModel.AddAxes(string.Empty, default, default, string.Empty, default, default);
      PlotModel.ApplyThemeToPlotModelAndAxes();

      this
        .WhenAny(vm => vm.Distribution, _ => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistribution
            )
          );

      this
        .WhenAny(vm => vm.Variable, vm => vm.Unit, (_, __) => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveParameter
            )
          );

      this
        .WhenAny(
          vm => vm.Alpha,
          vm => vm.Beta,
          vm => vm.Location,
          vm => vm.Scale,
          vm => vm.Lower,
          vm => vm.Upper,
          (_, __, ___, ____, _____, _______) => default(object)
          )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistributionParameters
            )
          );
    }

    public BetaScaledDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.BetaScaled;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<BetaScaledDistribution> Distribution
    {
      get => _betaScaledDistribution;
      set => this.RaiseAndSetIfChanged(ref _betaScaledDistribution, value, PropertyChanged);
    }
    private Option<BetaScaledDistribution> _betaScaledDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _betaScaledDistribution.Match(lnd => lnd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<BetaScaledDistribution>(value));
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

    public double? Alpha
    {
      get => _alpha;
      set => this.RaiseAndSetIfChanged(ref _alpha, value, PropertyChanged);
    }
    private double? _alpha;

    public double? Beta
    {
      get => _beta;
      set => this.RaiseAndSetIfChanged(ref _beta, value, PropertyChanged);
    }
    private double? _beta;

    public double? Location
    {
      get => _location;
      set => this.RaiseAndSetIfChanged(ref _location, value, PropertyChanged);
    }
    private double? _location;

    public double? Scale
    {
      get => _scale;
      set => this.RaiseAndSetIfChanged(ref _scale, value, PropertyChanged);
    }
    private double? _scale;

    public double? Lower
    {
      get => _lower;
      set => this.RaiseAndSetIfChanged(ref _lower, value, PropertyChanged);
    }
    private double? _lower;

    public double? Upper
    {
      get => _upper;
      set => this.RaiseAndSetIfChanged(ref _upper, value, PropertyChanged);
    }
    private double? _upper;

    public PlotModel PlotModel { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected override void ObserveThemeChange()
    {
      if (PlotModel != null)
      {
        PlotModel.ApplyThemeToPlotModelAndAxes();
        PlotModel.InvalidatePlot(false);
      }

      base.ObserveThemeChange();
    }

    private void ObserveDistribution(object? _)
    {
      Distribution.Match(
        betaScaled =>
        {
          Alpha = IsNaN(betaScaled.Alpha) ? default(double?) : betaScaled.Alpha;
          Beta = IsNaN(betaScaled.Beta) ? default(double?) : betaScaled.Beta;
          Location = IsNaN(betaScaled.Location) ? default(double?) : betaScaled.Location;
          Scale = IsNaN(betaScaled.Scale) ? default(double?) : betaScaled.Scale;
          Lower = IsNegativeInfinity(betaScaled.Lower) ? default(double?) : betaScaled.Lower;
          Upper = IsPositiveInfinity(betaScaled.Upper) ? default(double?) : betaScaled.Upper;
        },
        () =>
        {
          Alpha = default;
          Beta = default;
          Location = default;
          Scale = default;
          Lower = default;
          Upper = default;
        });

      UpdateAxes();
      UpdateSeries();
      PlotModel.ResetAllAxes();
      PlotModel.InvalidatePlot(true);
    }

    private void ObserveParameter(object? _)
    {
      UpdateAxes();
      PlotModel.InvalidatePlot(false);
    }

    private void ObserveDistributionParameters(object? _)
    {
      if (Alpha.HasValue && Beta.HasValue && Location.HasValue && Scale.HasValue)
      {
        if (Lower.HasValue || Upper.HasValue)
        {
          RequireTrue(AllowTruncation);

          Distribution = new BetaScaledDistribution(
            Alpha.Value,
            Beta.Value,
            Location.Value,
            Scale.Value,
            Lower ?? NegativeInfinity,
            Upper ?? PositiveInfinity
            );
        }
        else
        {
          Distribution = new BetaScaledDistribution(Alpha.Value, Beta.Value, Location.Value, Scale.Value);
        }
      }

      UpdateSeries();
      PlotModel.ResetAllAxes();
      PlotModel.InvalidatePlot(true);
    }

    private void UpdateAxes()
    {
      var variable = Variable ?? "?";

      var horizontalAxis = PlotModel.GetAxis(AxisPosition.Bottom).AssertNotNull();
      horizontalAxis.Title = variable;
      horizontalAxis.Unit = Unit;

      var verticalAxis = PlotModel.GetAxis(AxisPosition.Left).AssertNotNull();
      verticalAxis.Title = $"p({variable}|α,β,µ,σ)";
    }

    private void UpdateSeries()
    {
      PlotModel.Series.Clear();
      PlotModel.Annotations.Clear();

      if (Alpha.HasValue && Beta.HasValue && Location.HasValue && Scale.HasValue)
      {
        var betaScaled = Distribution.AssertSome().Implementation.AssertNotNull();

        var lineSeries = new LineSeries
        {
          Title = nameof(DistributionType.BetaScaled),
          LineStyle = LineStyle.Solid,
          Color = PlotModel.DefaultColors[0]
        };

        var interval = betaScaled.StdDev * 8;
        var lower = betaScaled.Mean - interval / 2.0;

        for (var i = 0; i < 101; ++i)
        {
          var x = lower + i * interval / 100.0;
          var y = betaScaled.Density(x);
          lineSeries.Points.Add(new DataPoint(x, y));
        }

        if (!lineSeries.Points.All(p => IsNaN(p.Y)))
        {
          PlotModel.Series.Add(lineSeries);

          RequireTrue(AllowTruncation || (!Lower.HasValue && !Upper.HasValue));

          AddLineAnnotation(Lower, nameof(Lower).ToLowerInvariant(), PlotModel);
          AddLineAnnotation(Upper, nameof(Upper).ToLowerInvariant(), PlotModel);
        }
      }
    }
  }
}
