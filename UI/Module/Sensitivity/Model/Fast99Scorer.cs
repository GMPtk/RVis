using LanguageExt;
using RVis.Base.Extensions;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace Sensitivity
{
  internal class Fast99Scorer : IScorer
  {
    internal Fast99Scorer(Map<string, (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance)> outputMeasures)
    {
      RequireFalse(outputMeasures.IsEmpty);

      _outputMeasures = outputMeasures;

      var (firstOrder, _, _) = _outputMeasures.Head().Value;

      _parameterNames = firstOrder.Columns
        .Cast<DataColumn>()
        .Skip(1)
        .Select(dc => dc.ColumnName)
        .ToArr();

      _independentData = Range(0, firstOrder.Rows.Count)
        .Map(i => firstOrder.Rows[i].Field<double>(0))
        .ToArr();
    }

    public Arr<(string ParameterName, double Score)> GetScores(double from, double to, Arr<string> outputs)
    {
      RequireTrue(from <= to);
      RequireFalse(outputs.IsEmpty);
      RequireTrue(outputs.ForAll(_outputMeasures.ContainsKey));

      outputs.Iter(ComputeScores);

      var indexFrom = IndexOfNearest(from);
      var indexTo = IndexOfNearest(to);

      return _parameterNames
        .Map(pn =>
        {
          var outputScores = outputs
            .Map(o =>
            {
              var (_, scores) = _scores[o]
                .Find(t => t.ParameterName == pn)
                .AssertSome();

              return Range(indexFrom, indexTo - indexFrom + 1)
                .Map(i => scores[i]);
            })
            .Bind(ds => ds);

          return (ParameterName: pn, Score: outputScores.Average());
        })
        .OrderByDescending(t => t.Score)
        .ToArr();
    }

    private int IndexOfNearest(double d)
    {
      var index = _independentData.FindIndex(id => d <= id);

      if (index.IsntFound())
      {
        index = _independentData.Count - 1;
      }
      else if (index > 0)
      {
        var lb = _independentData[index - 1];
        var rb = _independentData[index];
        var midPoint = lb + (rb - lb) / 2d;
        if (d < midPoint) --index;
      }

      return index;
    }

    private void ComputeScores(string output)
    {
      if (_scores.ContainsKey(output)) return;

      var (firstOrder, _, _) = _outputMeasures[output];

      var nXs = _independentData.Count;

      var scores = _parameterNames.Map(pn =>
      {
        var firstOrders = Range(0, nXs)
          .Map(i => firstOrder.Rows[i].Field<double>(pn))
          .ToArr();
        
        return (ParameterName: pn, Scores: firstOrders);
      });

      var maxScore = scores.Max(s => s.Scores.Max());
      scores = scores.Map(
        s => (s.ParameterName, s.Scores.Map(d => d / maxScore))
        );

      _scores.Add(output, scores);
    }

    private readonly Map<string, (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance)> _outputMeasures;
    private readonly IDictionary<string, Arr<(string ParameterName, Arr<double> Scores)>> _scores =
      new SortedDictionary<string, Arr<(string ParameterName, Arr<double> Scores)>>();
    private readonly Arr<string> _parameterNames;
    private readonly Arr<double> _independentData;
  }
}
