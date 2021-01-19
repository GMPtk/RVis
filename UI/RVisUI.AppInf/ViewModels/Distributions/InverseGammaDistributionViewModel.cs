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
  [DisplayName("Inverse Gamma")]
  public sealed class InverseGammaDistributionViewModel : DistributionViewModelBase, IInverseGammaDistributionViewModel, INotifyPropertyChanged
  {
    public InverseGammaDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
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
          (_, __) => default(object)
          )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistributionParameters
            )
          );
    }

    public InverseGammaDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.InverseGamma;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<InverseGammaDistribution> Distribution
    {
      get => _inverseGammaDistribution;
      set => this.RaiseAndSetIfChanged(ref _inverseGammaDistribution, value, PropertyChanged);
    }
    private Option<InverseGammaDistribution> _inverseGammaDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _inverseGammaDistribution.Match(lnd => lnd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<InverseGammaDistribution>(value));
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
        InverseGamma =>
        {
          Alpha = IsNaN(InverseGamma.Alpha) ? default(double?) : InverseGamma.Alpha;
          Beta = IsNaN(InverseGamma.Beta) ? default(double?) : InverseGamma.Beta;
        },
        () =>
        {
          Alpha = default;
          Beta = default;
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
        Distribution = new InverseGammaDistribution(Alpha.Value, Beta.Value);
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
      verticalAxis.Title = $"p({variable}|α,β)";
    }

    private void UpdateSeries()
    {
      PlotModel.Series.Clear();

      if (Alpha.HasValue && Beta.HasValue)
      {
        var inverseGamma = Distribution.AssertSome().Implementation.AssertNotNull();

        var n = 0;
        var incr = IsNaN(inverseGamma.StdDev) ? inverseGamma.Mode : inverseGamma.StdDev;
        var x = inverseGamma.Minimum;

        while (++n < 100)
        {
          x += incr * n;
          if (inverseGamma.CumulativeDistribution(x) >= 0.999) break;
        }

        var interval = x / 100.0;

        var lineSeries = new LineSeries
        {
          Title = nameof(DistributionType.InverseGamma),
          LineStyle = LineStyle.Solid,
          Color = PlotModel.DefaultColors[0]
        };

        for (var i = 0; i < 101; ++i)
        {
          x = inverseGamma.Minimum + i * interval;
          var y = inverseGamma.Density(x);
          lineSeries.Points.Add(new DataPoint(x, y));
        }

        if (!lineSeries.Points.All(p => IsNaN(p.Y)))
        {
          PlotModel.Series.Add(lineSeries);
        }
      }
    }
  }
}
