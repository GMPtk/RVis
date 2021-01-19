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
using static System.Math;

namespace RVisUI.AppInf
{
  [DisplayName("Log Normal")]
  public sealed class LogNormalDistributionViewModel : DistributionViewModelBase, ILogNormalDistributionViewModel, INotifyPropertyChanged
  {
    public LogNormalDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
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
          vm => vm.Mu,
          vm => vm.Sigma,
          vm => vm.Lower,
          vm => vm.Upper,
          (_, __, ___, ____) => default(object)
          )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistributionParameters
            )
          );

      this
        .WhenAny(vm => vm.Sigma, _ => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveSigma
            )
          );
    }

    public LogNormalDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.LogNormal;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<LogNormalDistribution> Distribution
    {
      get => _logNormalDistribution;
      set => this.RaiseAndSetIfChanged(ref _logNormalDistribution, value, PropertyChanged);
    }
    private Option<LogNormalDistribution> _logNormalDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _logNormalDistribution.Match(lnd => lnd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<LogNormalDistribution>(value));
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

    public double? Mu
    {
      get => _mu;
      set => this.RaiseAndSetIfChanged(ref _mu, value, PropertyChanged);
    }
    private double? _mu;

    public double? Sigma
    {
      get => _sigma;
      set
      {
        Var = value * value;
        this.RaiseAndSetIfChanged(ref _sigma, value, PropertyChanged);
      }
    }
    private double? _sigma;
    public double? Var
    {
      get => _var;
      private set => this.RaiseAndSetIfChanged(ref _var, value, PropertyChanged);
    }
    private double? _var;

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
      if (PlotModel != default)
      {
        PlotModel.ApplyThemeToPlotModelAndAxes();
        PlotModel.InvalidatePlot(false);
      }

      base.ObserveThemeChange();
    }

    private void ObserveDistribution(object? _)
    {
      Distribution.Match(
        logNormal =>
        {
          Mu = IsNaN(logNormal.Mu) ? default(double?) : logNormal.Mu;
          Sigma = IsNaN(logNormal.Sigma) ? default(double?) : logNormal.Sigma;
          Var = Sigma * Sigma;
          Lower = IsNegativeInfinity(logNormal.Lower) ? default(double?) : logNormal.Lower;
          Upper = IsPositiveInfinity(logNormal.Upper) ? default(double?) : logNormal.Upper;
        },
        () =>
        {
          Mu = default;
          Sigma = default;
          Var = default;
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
      if (Mu.HasValue && Sigma.HasValue)
      {
        if (Lower.HasValue || Upper.HasValue)
        {
          RequireTrue(AllowTruncation);

          Distribution = new LogNormalDistribution(
            Mu.Value,
            Sigma.Value,
            Lower ?? NegativeInfinity,
            Upper ?? PositiveInfinity
            );
        }
        else
        {
          var initializeBounds =
            AllowTruncation &&
            Distribution.Match(d => IsNaN(d.Sigma), () => true);

          if (initializeBounds)
          {
            Lower = Mu - 2 * Sigma;
            Upper = Mu + 2 * Sigma;
            Distribution = new LogNormalDistribution(Mu.Value, Sigma.Value, Lower.Value, Upper.Value);
          }
          else
          {
            Distribution = new LogNormalDistribution(Mu.Value, Sigma.Value);
          }
        }
      }

      UpdateSeries();
      PlotModel.ResetAllAxes();
      PlotModel.InvalidatePlot(true);
    }

    private void ObserveSigma(object? _) => Var = Sigma * Sigma;

    private void UpdateAxes()
    {
      var variable = Variable ?? "?";

      var horizontalAxis = PlotModel.GetAxis(AxisPosition.Bottom).AssertNotNull();
      horizontalAxis.Title = variable;
      horizontalAxis.Unit = Unit;

      var verticalAxis = PlotModel.GetAxis(AxisPosition.Left).AssertNotNull();
      verticalAxis.Title = $"φ_{{µ,σ²}} ({variable})";
    }

    private void UpdateSeries()
    {
      PlotModel.Series.Clear();
      PlotModel.Annotations.Clear();

      if (Mu.HasValue && Sigma.HasValue)
      {
        var distribution = Distribution.AssertSome();
        var logNormal = distribution.Implementation;
        var series = CreateLineSeries(
          logNormal.AssertNotNull(),
          Exp(distribution.Lower),
          Exp(distribution.Upper),
          PlotModel.DefaultColors
          );

        if (!series.Points.All(p => IsNaN(p.Y)))
        {
          PlotModel.Series.Add(series);

          if (Lower.HasValue)
          {
            RequireTrue(AllowTruncation);
            AddLineAnnotation(Exp(Lower.Value), nameof(Lower).ToLowerInvariant(), PlotModel);
          }

          if (Upper.HasValue)
          {
            RequireTrue(AllowTruncation);
            AddLineAnnotation(Exp(Upper.Value), nameof(Upper).ToLowerInvariant(), PlotModel);
          }
        }
      }
    }
  }
}
