using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
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
using static RVisUI.AppInf.DistributionLineSeries;
using static RVisUI.Wpf.WpfTools;
using static System.Double;

namespace RVisUI.AppInf
{
  [DisplayName("Gamma")]
  public sealed class GammaDistributionViewModel : DistributionViewModelBase, IGammaDistributionViewModel, INotifyPropertyChanged
  {
    public GammaDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
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

    public GammaDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.Gamma;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<GammaDistribution> Distribution
    {
      get => _gammaDistribution;
      set => this.RaiseAndSetIfChanged(ref _gammaDistribution, value, PropertyChanged);
    }
    private Option<GammaDistribution> _gammaDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _gammaDistribution.Match(lnd => lnd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<GammaDistribution>(value));
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
        gamma =>
        {
          Alpha = IsNaN(gamma.Alpha) ? default(double?) : gamma.Alpha;
          Beta = IsNaN(gamma.Beta) ? default(double?) : gamma.Beta;
          Lower = IsNegativeInfinity(gamma.Lower) ? default(double?) : gamma.Lower;
          Upper = IsPositiveInfinity(gamma.Upper) ? default(double?) : gamma.Upper;
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

          Distribution = new GammaDistribution(
            Alpha.Value,
            Beta.Value,
            Lower ?? NegativeInfinity,
            Upper ?? PositiveInfinity
            );
        }
        else
        {
          Distribution = new GammaDistribution(Alpha.Value, Beta.Value);
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
        var distribution = Distribution.AssertSome();
        var gamma = distribution.Implementation.AssertNotNull();
        var series = CreateLineSeries(
          gamma,
          distribution.Lower,
          distribution.Upper,
          PlotModel.DefaultColors
          );

        if (!series.Points.All(p => IsNaN(p.Y)))
        {
          PlotModel.Series.Add(series);

          RequireTrue(AllowTruncation || (!Lower.HasValue && !Upper.HasValue));

          AddLineAnnotation(Lower, nameof(Lower).ToLowerInvariant(), PlotModel);
          AddLineAnnotation(Upper, nameof(Upper).ToLowerInvariant(), PlotModel);
        }
      }
    }
  }
}
