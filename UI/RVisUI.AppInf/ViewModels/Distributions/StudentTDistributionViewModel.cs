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
  [DisplayName("Student's t")]
  public sealed class StudentTDistributionViewModel : DistributionViewModelBase, IStudentTDistributionViewModel, INotifyPropertyChanged
  {
    public StudentTDistributionViewModel(IAppService appService, IAppSettings appSettings) : base(appService, appSettings)
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
          vm => vm.Nu,
          vm => vm.Lower,
          vm => vm.Upper,
          (_, __, ___, ____, _____) => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveDistributionParameters
            )
          );
    }

    public StudentTDistributionViewModel()
      : this(new Design.AppService(), new Design.AppSettings())
    {
      RequireTrue(IsInDesignMode);
    }

    public DistributionType DistributionType => DistributionType.StudentT;

    public bool AllowTruncation
    {
      get => _allowTruncation;
      set => this.RaiseAndSetIfChanged(ref _allowTruncation, value, PropertyChanged);
    }
    private bool _allowTruncation;

    public Option<StudentTDistribution> Distribution
    {
      get => _studentTDistribution;
      set => this.RaiseAndSetIfChanged(ref _studentTDistribution, value, PropertyChanged);
    }
    private Option<StudentTDistribution> _studentTDistribution;

    public IDistribution? DistributionUnsafe
    {
      get => _studentTDistribution.Match(lnd => lnd, () => default(IDistribution));
      set => Distribution = value == default ? None : Some(RequireInstanceOf<StudentTDistribution>(value));
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

    public double? Nu
    {
      get => _nu;
      set => this.RaiseAndSetIfChanged(ref _nu, value, PropertyChanged);
    }
    private double? _nu;

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
        studentT =>
        {
          Mu = IsNaN(studentT.Mu) ? default(double?) : studentT.Mu;
          Sigma = IsNaN(studentT.Sigma) ? default(double?) : studentT.Sigma;
          Nu = IsNaN(studentT.Nu) ? default(double?) : studentT.Nu;
          Lower = IsNegativeInfinity(studentT.Lower) ? default(double?) : studentT.Lower;
          Upper = IsPositiveInfinity(studentT.Upper) ? default(double?) : studentT.Upper;
        },
        () =>
        {
          Mu = default;
          Sigma = default;
          Nu = default;
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
      if (Mu.HasValue && Sigma.HasValue && Nu.HasValue)
      {
        if (Lower.HasValue || Upper.HasValue)
        {
          RequireTrue(AllowTruncation);

          Distribution = new StudentTDistribution(
            Mu.Value,
            Sigma.Value,
            Nu.Value,
            Lower ?? NegativeInfinity,
            Upper ?? PositiveInfinity
            );
        }
        else
        {
          Distribution = new StudentTDistribution(Mu.Value, Sigma.Value, Nu.Value);
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
      verticalAxis.Title = $"P({variable})";
    }

    private void UpdateSeries()
    {
      PlotModel.Series.Clear();
      PlotModel.Annotations.Clear();

      if (Mu.HasValue && Sigma.HasValue && Nu.HasValue)
      {
        var studentT = Distribution.AssertSome().Implementation.AssertNotNull();

        var n = 0;
        var incr = IsNaN(studentT.StdDev) ? studentT.Scale / 10.0 : studentT.StdDev;
        var x = studentT.Mode;

        while (++n < 100)
        {
          x += incr * n;
          if (studentT.CumulativeDistribution(x) >= 0.999) break;
        }

        var start = studentT.Mode - (x - studentT.Mode);
        var interval = (x - start) / 100.0;

        var lineSeries = new LineSeries
        {
          Title = nameof(DistributionType.InverseGamma),
          LineStyle = LineStyle.Solid,
          Color = PlotModel.DefaultColors[0]
        };

        for (var i = 0; i < 101; ++i)
        {
          x = start + i * interval;
          var y = studentT.Density(x);
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
