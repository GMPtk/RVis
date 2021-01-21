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
  [DisplayName("Beta")]
  public sealed class BetaDistributionViewModel : DistributionViewModelBase, IBetaDistributionViewModel, INotifyPropertyChanged
  {
    public BetaDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
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
          vm => vm.Lower,
          vm => vm.Upper,
          (_, __, ___, ____) => default(object)
          )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistributionParameters
            )
          );
    }

    public BetaDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.Beta;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<BetaDistribution> Distribution
    {
      get => _betaDistribution;
      set => this.RaiseAndSetIfChanged(ref _betaDistribution, value, PropertyChanged);
    }
    private Option<BetaDistribution> _betaDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _betaDistribution.Match(d => d, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<BetaDistribution>(value));
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
        beta =>
        {
          Alpha = IsNaN(beta.Alpha) ? default(double?) : beta.Alpha;
          Beta = IsNaN(beta.Beta) ? default(double?) : beta.Beta;
          Lower = IsNegativeInfinity(beta.Lower) ? default(double?) : beta.Lower;
          Upper = IsPositiveInfinity(beta.Upper) ? default(double?) : beta.Upper;
        },
        () =>
        {
          Alpha = default;
          Beta = default;
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
      if (Alpha.HasValue && Beta.HasValue)
      {
        if (Lower.HasValue || Upper.HasValue)
        {
          RequireTrue(AllowTruncation);

          Distribution = new BetaDistribution(
            Alpha.Value,
            Beta.Value,
            Lower ?? NegativeInfinity,
            Upper ?? PositiveInfinity
            );
        }
        else
        {
          Distribution = new BetaDistribution(Alpha.Value, Beta.Value);
        }
      }

      UpdateSeries();
      PlotModel.ResetAllAxes();
      PlotModel.InvalidatePlot(true);
    }

    private void UpdateAxes()
    {
      var variable = Variable ?? "?";

      var horizontalAxis = PlotModel.GetAxis(AxisPosition.Bottom);
      horizontalAxis.Title = variable;
      horizontalAxis.Unit = Unit;

      var verticalAxis = PlotModel.GetAxis(AxisPosition.Left);
      verticalAxis.Title = $"p({variable}|α,β)";
    }

    private void UpdateSeries()
    {
      PlotModel.Series.Clear();
      PlotModel.Annotations.Clear();

      if (Alpha.HasValue && Beta.HasValue)
      {
        var beta = Distribution.AssertSome().Implementation.AssertNotNull();

        var lineSeries = new LineSeries
        {
          Title = nameof(DistributionType.Beta),
          LineStyle = LineStyle.Solid,
          Color = PlotModel.DefaultColors[0]
        };

        for (var i = 0; i < 101; ++i)
        {
          var x = i / 100.0;
          var y = beta.Density(x);
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
