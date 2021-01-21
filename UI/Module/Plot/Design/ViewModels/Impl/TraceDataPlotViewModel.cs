using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using RVis.Data;
using System.Windows.Input;
using static LanguageExt.Prelude;

#nullable disable

namespace Plot.Design
{
  public class TraceDataPlotViewModel : ITraceDataPlotViewModel
  {
    public TraceDataPlotViewModel()
    {
      State = ModuleState.DefaultTraceDataPlotStates[0];
    }

    public TraceDataPlotViewModel(TraceDataPlotState state)
    {
      State = state;
    }

    public TraceDataPlotState State { get; }

    public Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> DataSet
    {
      get => Array((Data.HPTorqueOverRPM.Name, Array((default(string), Data.HPTorqueOverRPM))));
      set { }
    }
    public Arr<string> SeriesNames
    {
      get => Array(
        "Data Set 00000001",
        "Data Set 00000002 Data Set 00000002",
        "Data Set 00000003 Data Set 00000003 Data Set 00000003",
        "Data Set 00000004"
      );
      set { }
    }
    public int SelectedIndexSeries { get => 2; set { } }

    public IDepVarConfigViewModel DepVarConfigViewModel => new DepVarConfigViewModel();

    public PlotModel PlotModel
    {
      get
      {
        var hpTorqueOverRPM = Data.HPTorqueOverRPM;

        var plotModel = new PlotModel
        {
          Background = OxyColors.Transparent,
          LegendPosition = LegendPosition.BottomRight
        };

        var traceHorizontalAxis = new LinearAxis
        {
          Position = AxisPosition.Bottom,
          MinimumPadding = 0,
          MaximumPadding = 0.06,
          AbsoluteMinimum = 0.0,
          Minimum = 0.0,
        };
        plotModel.Axes.Add(traceHorizontalAxis);

        var rpmColumn = hpTorqueOverRPM.NumDataColumns[0];
        var nPoints = rpmColumn.Length;

        traceHorizontalAxis.Title = rpmColumn.Name;
        traceHorizontalAxis.Unit = "N.m";

        var hpPerRpm = hpTorqueOverRPM.NumDataColumns[1];

        var traceVerticalAxis = new LinearAxis
        {
          Position = AxisPosition.Left,
          MinimumPadding = 0,
          MaximumPadding = 0.06,
          AbsoluteMinimum = 0.0,
          Minimum = 0.0,
          Key = hpPerRpm.Name,
          Title = hpPerRpm.Name,
          Unit = "HP/rpm"
        };
        plotModel.Axes.Add(traceVerticalAxis);

        var lineSeries = new LineSeries
        {
          Title = hpPerRpm.Name,
          MarkerType = MarkerType.Circle,
          InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline,
          YAxisKey = hpPerRpm.Name
        };

        for (var i = 0; i < nPoints; ++i)
        {
          lineSeries.Points.Add(new DataPoint(rpmColumn[i], hpPerRpm[i]));
        }

        plotModel.Series.Add(lineSeries);

        return plotModel;
      }
      set { }
    }

    public ICommand ToggleSeriesType => default;

    public bool IsSeriesTypeLine { get => true; set { } }

    public ICommand ToggleLockAxesOriginToZeroZero => default;

    public bool IsAxesOriginLockedToZeroZero { get => true; set { } }

    public ICommand ResetAxisRanges => default;

    public ICommand RemoveChart => default;
  }
}
