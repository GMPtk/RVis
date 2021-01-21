using LanguageExt;
using MaterialDesignThemes.Wpf;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using static MathNet.Numerics.Statistics.SortedArrayStatistics;
using static RVis.Base.Check;
using static RVisUI.Wpf.WpfTools;
using static System.Array;
using static System.Convert;
using static System.Double;
using static System.Math;

namespace RVisUI.AppInf.Extensions
{
  public static class PlotExt
  {
    public static readonly Arr<string> ColorNames = typeof(OxyColors)
      .GetFields()
      .Skip(1)
      .Map(fi => fi.Name)
      .ToArr();

    public static readonly Arr<string> MarkerNames = Enum
      .GetNames(typeof(MarkerType))
      .Skip(1)
      .ToArr();

    public static readonly Arr<MarkerType> MarkerTypes = Enum
      .GetValues(typeof(MarkerType))
      .Cast<MarkerType>()
      .ToArr()
      .Filter(mt => mt != MarkerType.Custom && mt != MarkerType.None);

    public static (double BinWidth, int NBins) FreedmanDiaconis(this Arr<double> samples)
    {
      RequireFalse(samples.IsEmpty);

      var array = samples.ToArray();
      return GetFreedmanDiaconis(array);
    }
    public static (double BinWidth, int NBins) FreedmanDiaconis(this double[] samples)
    {
      RequireFalse(samples.IsNullOrEmpty());

      var array = (double[])samples.Clone();
      return GetFreedmanDiaconis(array);
    }

    public static Axis GetAxis(this PlotModel plotModel, AxisPosition axisPosition) =>
      plotModel.Axes
        .SingleOrDefault(a => a.Position == axisPosition)
        .AssertNotNull($"{axisPosition} axis not found on {plotModel.Title ?? "PlotModel"}");

    public static void AssignDefaultColors(
      this PlotModel plotModel,
      bool isBaseDark,
      int nSeries
      )
    {
      var palette = isBaseDark
        ? OxyPalettes.Cool(nSeries)
        : OxyPalettes.Rainbow(nSeries);

      plotModel.DefaultColors = palette.Colors;
    }

    public static ScatterSeries AddScatterSeries(
      this PlotModel plotModel,
      int seriesIndex,
      string seriesName,
      Arr<double> x,
      Arr<double> y,
      IList<OxyColor> colors,
      string? yAxisKey,
      object? tag
      )
    {
      var markerType = MarkerTypes[seriesIndex % MarkerTypes.Count];

      var markerStroke =
        markerType == MarkerType.Cross ||
        markerType == MarkerType.Plus ||
        markerType == MarkerType.Star
        ? colors[seriesIndex % colors.Count]
        : OxyColors.Black;

      var scatterSeries = new ScatterSeries
      {
        MarkerStroke = markerStroke,
        MarkerFill = colors[seriesIndex % colors.Count],
        MarkerType = markerType,
        Title = seriesName
      };

      for (var i = 0; i < x.Count; ++i)
      {
        scatterSeries.Points.Add(new ScatterPoint(x[i], y[i]));
      }

      scatterSeries.YAxisKey = yAxisKey;
      scatterSeries.Tag = tag;

      plotModel.Series.Add(scatterSeries);

      return scatterSeries;
    }

    public static void ApplyThemeToPlotModelAndAxes(this PlotModel plotModel)
    {
      ITheme theme;
      try
      {
        theme = new PaletteHelper().GetTheme();
      }
      catch (Exception)
      {
        if (!IsInDesignMode) throw;
        return;
      }

      var plotColor = OxyColor.FromArgb(
        theme.PrimaryMid.Color.A,
        theme.PrimaryMid.Color.R,
        theme.PrimaryMid.Color.G,
        theme.PrimaryMid.Color.B
        );

      plotModel.PlotAreaBorderColor = plotColor;

      foreach (var axis in plotModel.Axes)
      {
        axis.TicklineColor = plotColor;
      }

      plotModel.TextColor = OxyColor.FromArgb(
        theme.Body.A,
        theme.Body.R,
        theme.Body.G,
        theme.Body.B
        );

      foreach (var lineAnnotation in plotModel.Annotations.OfType<LineAnnotation>())
      {
        lineAnnotation.Color = plotModel.TextColor;
      }

      // oxyplot v2.1???

      //var background = OxyColor.FromArgb(
      //  theme.Paper.A,
      //  theme.Paper.R,
      //  theme.Paper.G,
      //  theme.Paper.B
      //  );

      //foreach (var legend in plotModel.Legends)
      //{
      //  legend.LegendBackground = background;
      //}
    }

    public static void AddAxes(
      this PlotModel plotModel,
      string xTitle,
      double? xMin,
      double? xMax,
      string? yTitle,
      double? yMin,
      double? yMax
    )
    {
      RequireTrue(plotModel.Axes.Count == 0);

      var horizontalAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom,
        Title = xTitle,
        TicklineColor = plotModel.TextColor,
        Minimum = xMin ?? NaN,
        Maximum = xMax ?? NaN
      };

      plotModel.Axes.Add(horizontalAxis);

      var verticalAxis = new LinearAxis
      {
        Position = AxisPosition.Left,
        Title = yTitle,
        TicklineColor = plotModel.TextColor,
        Minimum = yMin ?? NaN,
        Maximum = yMax ?? NaN
      };

      plotModel.Axes.Add(verticalAxis);
    }

    private static (double BinWidth, int NBins) GetFreedmanDiaconis(double[] array)
    {
      Sort(array);

      var iqr = InterquartileRange(array);
      if (iqr == 0.0) return (PositiveInfinity, 1);

      var binWidth = 2.0 * iqr * Pow(array.Length, -1.0 / 3.0);
      var nBins = ToInt32((array[^1] - array[0]) / binWidth);
      return (binWidth, nBins);
    }
  }
}
