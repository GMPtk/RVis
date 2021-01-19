using LanguageExt;
using MathNet.Numerics.Distributions;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Double;

namespace RVisUI.AppInf
{
  internal static class DistributionLineSeries
  {
    internal static LineSeries CreateLineSeries(
      Normal normal,
      double lower,
      double upper,
      IList<OxyColor> defaultColors
      ) =>
      CreateLineSeries(
        normal,
        lower,
        upper,
        defaultColors,
        d => normal.InverseCumulativeDistribution(d),
        InterpolationAlgorithms.CatmullRomSpline
        );

    internal static LineSeries CreateLineSeries(
      LogNormal logNormal,
      double lower,
      double upper,
      IList<OxyColor> defaultColors
      ) =>
      CreateLineSeries(
        logNormal,
        lower,
        upper,
        defaultColors,
        d => logNormal.InverseCumulativeDistribution(d),
        InterpolationAlgorithms.CatmullRomSpline
        );

    internal static LineSeries CreateLineSeries(
      ContinuousUniform continuousUniform,
      double lower,
      double upper,
      IList<OxyColor> defaultColors
      ) =>
      CreateLineSeries(
        continuousUniform,
        lower,
        upper,
        defaultColors,
        d => continuousUniform.InverseCumulativeDistribution(d),
        default
        );

    internal static LineSeries CreateLineSeries(
      Gamma gamma,
      double lower,
      double upper,
      IList<OxyColor> defaultColors
      ) =>
      CreateLineSeries(
        gamma,
        lower,
        upper,
        defaultColors,
        d => gamma.InverseCumulativeDistribution(d),
        default
        );

    private static LineSeries CreateLineSeries<T>(
      T continuousDistribution,
      double lower,
      double upper,
      IList<OxyColor> defaultColors,
      Func<double, double> inverseCumulativeDistribution,
      IInterpolationAlgorithm? interpolationAlgorithm
      ) where T : IContinuousDistribution
    {
      var hasLowerBound = lower > NegativeInfinity;
      var hasUpperBound = upper < PositiveInfinity;

      if (!hasLowerBound) lower = inverseCumulativeDistribution(0.0005);
      if (!hasUpperBound) upper = inverseCumulativeDistribution(0.9995);

      if (hasLowerBound || hasUpperBound)
      {
        var padding = (upper - lower) / 10d;
        if (hasLowerBound) lower -= padding;
        if (hasUpperBound) upper += padding;
      }

      RequireTrue(lower < upper, $"Configured {typeof(T).Name} has zero width");

      const int nPoints = 100;
      var division = (upper - lower) / nPoints;
      var x = Range(0, nPoints + 1).Map(i => lower + i * division).ToArr();
      var densities = x.Map(continuousDistribution.Density);
      RequireFalse(densities.ForAll(IsNaN), $"Configured {typeof(T).Name} has undefined density in range {lower} to {upper}");

      return CreateLineSeries(
        0,
        typeof(T).Name,
        x,
        densities,
        defaultColors,
        LineStyle.Solid,
        interpolationAlgorithm
        );
    }

    private static LineSeries CreateLineSeries(
      int seriesIndex,
      string seriesName,
      Arr<double> x,
      Arr<double> y,
      IList<OxyColor> colors,
      LineStyle lineStyle,
      IInterpolationAlgorithm? interpolationAlgorithm
    )
    {
      var lineSeries = new LineSeries
      {
        Title = seriesName,
        LineStyle = lineStyle,
        Color = colors[seriesIndex % colors.Count],
        InterpolationAlgorithm = interpolationAlgorithm
      };

      for (var i = 0; i < x.Count; ++i)
      {
        lineSeries.Points.Add(new DataPoint(x[i], y[i]));
      }

      return lineSeries;
    }
  }
}
