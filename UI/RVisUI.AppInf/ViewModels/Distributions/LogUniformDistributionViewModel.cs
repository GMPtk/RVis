using LanguageExt;
using MathNet.Numerics.Statistics;
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
  [DisplayName("Log Uniform")]
  public sealed class LogUniformDistributionViewModel : DistributionViewModelBase, ILogUniformDistributionViewModel, INotifyPropertyChanged
  {
    public LogUniformDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
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
        .WhenAny(vm => vm.Lower, vm => vm.Upper, (_, __) => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistributionParameters
            )
          );
    }

    public LogUniformDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.LogUniform;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<LogUniformDistribution> Distribution
    {
      get => _logUniformDistribution;
      set => this.RaiseAndSetIfChanged(ref _logUniformDistribution, value, PropertyChanged);
    }
    private Option<LogUniformDistribution> _logUniformDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _logUniformDistribution.Match(lnd => lnd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<LogUniformDistribution>(value));
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
        Distribution = new LogUniformDistribution(Lower.Value, Upper.Value);
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
        var samples = new double[100000];
        Distribution.AssertSome().FillSamples(samples);
        var histogram = new Histogram(samples, 20);

        var lineSeries = new LineSeries
        {
          Title = nameof(DistributionType.LogUniform),
          LineStyle = LineStyle.Solid,
          Color = PlotModel.DefaultColors[0],
          InterpolationAlgorithm = InterpolationAlgorithms.UniformCatmullRomSpline,
        };

        for (var i = 0; i < histogram.BucketCount; ++i)
        {
          var bucket = histogram[i];
          var x = bucket.LowerBound + (bucket.UpperBound - bucket.LowerBound) / 2.0;
          var y = bucket.Count / samples.Length;
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
