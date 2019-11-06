using RVis.Base.Extensions;
using System.Collections.Generic;
using System.Linq;
using static MathNet.Numerics.Interpolate;
using static RVis.Base.Check;

namespace Estimation
{
  internal static class CollExt
  {
    internal static IEnumerable<double> ApproximateY(
      this IEnumerable<double> targetX,
      IEnumerable<double> sourceX,
      IEnumerable<double> sourceY      
      )
    {
      var interpolation = Linear(sourceX, sourceY);
      return targetX.Select(interpolation.Interpolate);
    }

    internal static IEnumerable<double> SelectNearestY(
      this IReadOnlyList<double> targetX,
      IReadOnlyList<double> sourceX,
      IReadOnlyList<double> sourceY
      )
    {
      var indices = targetX
        .Map(x =>
        {
          var index = sourceX.FindIndex(xx => x <= xx);
          if (index == 0) return 0;
          if (index.IsntFound()) return sourceX.Count - 1;

          var nextX = sourceX[index];
          if (nextX == x) return index;

          var previousX = sourceX[index - 1];

          var midway = previousX + (nextX - previousX) / 2d;
          return x > midway ? index : index - 1;
        });

      RequireUniqueElements(indices, i => i, "Failed to match unique source Xs for target Xs");

      return indices.Select(i => sourceY[i]);
    }
  }
}
