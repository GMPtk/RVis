using LanguageExt;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static RVis.Base.Check;

namespace Sensitivity
{
  internal struct MuStarSigmaParameterMeasure
  {
    internal MuStarSigmaParameterMeasure(string parameterName, double muStar, double sigma)
    {
      ParameterName = parameterName;
      MuStar = muStar;
      Sigma = sigma;
    }

    internal string ParameterName { get; }
    internal double MuStar { get; }
    internal double Sigma { get; }
  }

  internal struct MuStarSigmaOutputMeasures
  {
    internal MuStarSigmaOutputMeasures(
      Arr<MuStarSigmaParameterMeasure> parameterMeasures,
      double outputValue
      )
    {
      ParameterMeasures = parameterMeasures;
      OutputValue = outputValue;
    }

    internal Arr<MuStarSigmaParameterMeasure> ParameterMeasures { get; }
    internal double OutputValue { get; }
  }

  internal static class MuStarSigmaPlotData
  {
    internal static IDictionary<double, MuStarSigmaOutputMeasures> CompileOutputMeasures(
      (DataTable Mu, DataTable MuStar, DataTable Sigma) measures,
      IReadOnlyList<double> traceIndependent,
      IReadOnlyList<double> traceDependent
      )
    {
      var (_, muStar, sigma) = measures;

      RequireTrue(muStar.Rows.Count == traceIndependent.Count);

      var parameterNames = muStar.Columns
        .Cast<DataColumn>()
        .Skip(1)
        .Select(dc => dc.ColumnName)
        .ToArr();

      RequireFalse(parameterNames.IsEmpty);

      var nZeroes = traceIndependent.TakeWhile(d => d == 0.0).Count();

      var outputMeasures = new SortedDictionary<double, MuStarSigmaOutputMeasures>();

      for (var i = nZeroes; i < traceIndependent.Count; ++i)
      {
        var x = traceIndependent[i];
        if (outputMeasures.ContainsKey(x)) continue;

        var parameterMeasures = new MuStarSigmaParameterMeasure[parameterNames.Count];

        for (var j = 0; j < parameterNames.Count; ++j)
        {
          var parameterMeasure = new MuStarSigmaParameterMeasure(
            parameterNames[j],
            muStar.Rows[i].Field<double>(j + 1),
            sigma.Rows[i].Field<double>(j + 1)
            );

          parameterMeasures[j] = parameterMeasure;
        }

        outputMeasures.Add(
          x,
          new MuStarSigmaOutputMeasures(parameterMeasures.ToArr(), traceDependent[i])
          );
      }

      return outputMeasures;
    }
  }
}