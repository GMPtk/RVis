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
  [DisplayName("Normal")]
  public sealed class NormalDistributionViewModel : DistributionViewModelBase, INormalDistributionViewModel, INotifyPropertyChanged
  {
    public NormalDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
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

    public NormalDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.Normal;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<NormalDistribution> Distribution
    {
      get => _normalDistribution;
      set => this.RaiseAndSetIfChanged(ref _normalDistribution, value, PropertyChanged);
    }
    private Option<NormalDistribution> _normalDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _normalDistribution.Match(nd => nd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<NormalDistribution>(value));
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
      set => this.RaiseAndSetIfChanged(ref _sigma, value, PropertyChanged);
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
        normal =>
        {
          Mu = IsNaN(normal.Mu) ? default(double?) : normal.Mu;
          Sigma = IsNaN(normal.Sigma) ? default(double?) : normal.Sigma;
          Var = Sigma * Sigma;
          Lower = IsNegativeInfinity(normal.Lower) ? default(double?) : normal.Lower;
          Upper = IsPositiveInfinity(normal.Upper) ? default(double?) : normal.Upper;
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

          Distribution = new NormalDistribution(
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
            Distribution = new NormalDistribution(Mu.Value, Sigma.Value, Lower.Value, Upper.Value);
          }
          else
          {
            Distribution = new NormalDistribution(Mu.Value, Sigma.Value);
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
        var normal = distribution.Implementation.AssertNotNull();
        var series = CreateLineSeries(
          normal,
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
