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
    internal static LineSeries CreateLineSeries(Normal normal, IList<OxyColor> defaultColors) =>
      CreateLineSeries(
        normal,
        d => normal.InverseCumulativeDistribution(d),
        defaultColors,
        InterpolationAlgorithms.CatmullRomSpline
        );

    internal static LineSeries CreateLineSeries(LogNormal logNormal, IList<OxyColor> defaultColors) =>
      CreateLineSeries(
        logNormal,
        d => logNormal.InverseCumulativeDistribution(d),
        defaultColors,
        InterpolationAlgorithms.CatmullRomSpline
        );

    internal static LineSeries CreateLineSeries(ContinuousUniform continuousUniform, IList<OxyColor> defaultColors) =>
      CreateLineSeries(
        continuousUniform,
        d => continuousUniform.InverseCumulativeDistribution(d),
        defaultColors,
        default
        );

    internal static LineSeries CreateLineSeries(Beta beta, IList<OxyColor> defaultColors) =>
      CreateLineSeries(
        beta,
        d => beta.InverseCumulativeDistribution(d),
        defaultColors,
        default
        );

    internal static LineSeries CreateLineSeries(Gamma gamma, IList<OxyColor> defaultColors) =>
      CreateLineSeries(
        gamma,
        d => gamma.InverseCumulativeDistribution(d),
        defaultColors,
        default
        );

    private static LineSeries CreateLineSeries<T>(
      T continuousDistribution,
      Func<double, double> inverseCumulativeDistribution,
      IList<OxyColor> defaultColors,
      IInterpolationAlgorithm interpolationAlgorithm
      ) where T : IContinuousDistribution
    {
      var cdLower = inverseCumulativeDistribution(0.0005);
      var cdUpper = inverseCumulativeDistribution(0.9995);
      RequireTrue(cdLower < cdUpper, $"Configured {typeof(T).Name} has zero width");

      const int nPoints = 100;
      var division = (cdUpper - cdLower) / nPoints;
      var x = Range(0, nPoints + 1).Map(i => cdLower + i * division).ToArr();
      var densities = x.Map(continuousDistribution.Density);
      RequireFalse(densities.ForAll(IsNaN), $"Configured {typeof(T).Name} has undefined density in range {cdLower} to {cdUpper}");

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
      IInterpolationAlgorithm interpolationAlgorithm
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
