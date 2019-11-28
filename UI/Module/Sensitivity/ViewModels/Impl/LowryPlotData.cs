using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static RVis.Base.Check;
using static System.Double;

namespace Sensitivity
{
  internal struct LowryParameterMeasure
  {
    internal LowryParameterMeasure(string parameterName)
    {
      ParameterName = parameterName;
      MainEffect = NaN;
      Interaction = NaN;
      LowerBound = NaN;
      UpperBound = NaN;
    }

    internal string ParameterName { get; }
    internal double MainEffect { get; set; }
    internal double Interaction { get; set; }
    internal double LowerBound { get; set; }
    internal double UpperBound { get; set; }
  }

  internal struct LowryOutputMeasures
  {
    internal LowryOutputMeasures(
      Arr<LowryParameterMeasure> parameterMeasures,
      double outputValue
      )
    {
      ParameterMeasures = parameterMeasures;
      OutputValue = outputValue;
    }

    internal Arr<LowryParameterMeasure> ParameterMeasures { get; }
    internal double OutputValue { get; }
  }

  internal static class LowryPlotData
  {
    internal static IDictionary<double, LowryOutputMeasures> CompileOutputMeasures(
      (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures,
      IReadOnlyList<double> traceIndependent,
      IReadOnlyList<double> traceDependent
      )
    {
      var (firstOrder, totalOrder, _) = measures;

      RequireTrue(firstOrder.Rows.Count == traceIndependent.Count);

      var parameterNames = firstOrder.Columns
        .Cast<DataColumn>()
        .Skip(1)
        .Select(dc => dc.ColumnName)
        .ToArr();

      RequireFalse(parameterNames.IsEmpty);

      var nZeroes = traceIndependent.TakeWhile(d => d == 0.0).Count();

      var outputMeasures = new SortedDictionary<double, LowryOutputMeasures>();

      for (var i = nZeroes; i < traceIndependent.Count; ++i)
      {
        var x = traceIndependent[i];
        if (outputMeasures.ContainsKey(x)) continue;

        var parameterMeasures = new LowryParameterMeasure[parameterNames.Count];

        var totalEffects = 0.0;

        for (var j = 0; j < parameterNames.Count; ++j)
        {
          var parameterMeasure = new LowryParameterMeasure(parameterNames[j]);

          if (i != 0)
          {
            var mainEffect = firstOrder.Rows[i].Field<double>(j + 1);
            var totalEffect = totalOrder.Rows[i].Field<double>(j + 1);
            parameterMeasure.MainEffect = mainEffect;
            parameterMeasure.Interaction = totalEffect - mainEffect;

            totalEffects += parameterMeasure.MainEffect + parameterMeasure.Interaction;
          }

          parameterMeasures[j] = parameterMeasure;
        }

        for (var j = 0; j < parameterMeasures.Length; ++j)
        {
          parameterMeasures[j].MainEffect /= totalEffects;
          parameterMeasures[j].Interaction /= totalEffects;
        }

        Array.Sort(
          parameterMeasures,
          (a, b) => Comparer<double>.Default.Compare(b.MainEffect, a.MainEffect)
          );

        var mainEffects = parameterMeasures.Select(pd => pd.MainEffect).ToArray();
        var interactions = parameterMeasures.Select(pd => pd.Interaction).ToArray();

        var upperBounds = ComputeUpperBounds(mainEffects, interactions);
        var lowerBounds = ComputeLowerBounds(mainEffects);

        for (var j = 0; j < parameterMeasures.Length; ++j)
        {
          parameterMeasures[j].LowerBound = lowerBounds[j];
          parameterMeasures[j].UpperBound = upperBounds[j];
        }

        outputMeasures.Add(
          x,
          new LowryOutputMeasures(parameterMeasures.ToArr(), traceDependent[i])
          );
      }

      return outputMeasures;
    }

    internal static double[] ComputeUpperBounds(double[] mainEffects, double[] interactions)
    {
      RequireFalse(mainEffects.IsNullOrEmpty());
      RequireFalse(interactions.IsNullOrEmpty());

      // Rule1: Cumulative sum of total effects
      var mERest = mainEffects.Skip(1).ToArray();
      var intRest = interactions.Skip(1).ToArray();

      var ub1 = mERest
        .Zip(intRest, (me, r) => (me, r))
        .Aggregate(
          new List<double> { mainEffects[0] + interactions[0] },
          (l, t) =>
          {
            l.Add(l[l.Count - 1] + t.me + t.r);
            return l;
          });

      // Rule2: 1 minus interactions not included.
      // (err, wat?)
      // (not RC's JS but a transcription of his R from McNally et al. (2011))
      var cumSumME = mainEffects
        .Reverse()
        .Aggregate(
          new List<double> { 0.0 },
          (l, me) =>
          {
            l.Add(l[l.Count - 1] + me);
            return l;
          });

      var ub2 = cumSumME
        .Skip(1)
        .Reverse()
        .Skip(1)
        .Select(me => 1.0 - me)
        .Concat(new[] { 1.0 })
        .ToArray();

      // Now take the minimum of each rule at each point
      return ub1
        .Zip(ub2, (a, b) => Math.Min(a, b))
        .ToArray();
    }

    internal static double[] ComputeLowerBounds(double[] mainEffects)
    {
      RequireFalse(mainEffects.IsNullOrEmpty());

      return mainEffects
        .Skip(1)
        .Aggregate(
          new List<double> { mainEffects[0] },
          (a, n) =>
          {
            a.Add(a[a.Count - 1] + n);
            return a;
          })
        .ToArray();
    }
  }
}