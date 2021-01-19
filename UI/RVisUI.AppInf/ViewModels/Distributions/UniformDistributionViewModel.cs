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
  [DisplayName("Uniform")]
  public sealed class UniformDistributionViewModel : DistributionViewModelBase, IUniformDistributionViewModel, INotifyPropertyChanged
  {
    public UniformDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
    {
      PlotModel = new PlotModel
      {
        IsLegendVisible = false
      };
      PlotModel.AddAxes(string.Empty, default, default, string.Empty, 0.0, default);
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
        .WhenAny(vm => vm.Lower, vm => vm.Upper, (_, __) => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistributionParameters
            )
          );
    }

    public UniformDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.Uniform;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<UniformDistribution> Distribution
    {
      get => _uniformDistribution;
      set => this.RaiseAndSetIfChanged(ref _uniformDistribution, value, PropertyChanged);
    }
    private Option<UniformDistribution> _uniformDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _uniformDistribution.Match(lnd => lnd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<UniformDistribution>(value));
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
        uniform =>
        {
          Lower = IsNaN(uniform.Lower) ? default(double?) : uniform.Lower;
          Upper = IsNaN(uniform.Upper) ? default(double?) : uniform.Upper;
        },
        () =>
        {
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
      if (Lower.HasValue && Upper.HasValue)
      {
        Distribution = new UniformDistribution(Lower.Value, Upper.Value);
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
      verticalAxis.Title = $"p({variable})";
    }

    private void UpdateSeries()
    {
      PlotModel.Series.Clear();

      if (Lower.HasValue && Upper.HasValue)
      {
        var distribution = Distribution.AssertSome();
        var uniform = distribution.Implementation.AssertNotNull();
        var series = CreateLineSeries(
          uniform,
          distribution.Lower,
          distribution.Upper,
          PlotModel.DefaultColors
          );
        series.Points.Insert(0, new DataPoint(uniform.LowerBound, 0.0));
        series.Points.Add(new DataPoint(uniform.UpperBound, 0.0));

        if (series.Points.All(p => IsNaN(p.Y))) return;

        PlotModel.Series.Add(series);

        var range = uniform.UpperBound - uniform.LowerBound;

        var horizontalAxis = PlotModel.GetAxis(AxisPosition.Bottom).AssertNotNull();
        horizontalAxis.Minimum = uniform.LowerBound - range;
        horizontalAxis.Maximum = uniform.UpperBound + range;

        var verticalAxis = PlotModel.GetAxis(AxisPosition.Left).AssertNotNull();
        verticalAxis.Maximum = 2.0 * uniform.Density(uniform.LowerBound + range / 2.0);
      }
    }
  }
}
