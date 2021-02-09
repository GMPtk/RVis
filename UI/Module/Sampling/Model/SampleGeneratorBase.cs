using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.AppInf;
using RVisUI.AppInf.Extensions;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static System.Double;
using DataTable = System.Data.DataTable;

namespace Sampling
{
  internal abstract class SampleGeneratorBase : ISampleGenerator
  {
    internal SampleGeneratorBase(
      Arr<ParameterState> parameterStates,
      RankCorrelationDesign rankCorrelationDesign,
      int nSamples,
      int? seed,
      IRVisServerPool serverPool
      )
    {
      RequireOrdered(parameterStates, ps => ps.Name.ToUpperInvariant());

      _parameterStates = parameterStates;
      _rankCorrelationDesign = rankCorrelationDesign;
      _nSamples = nSamples;
      _seed = seed;
      _serverPool = serverPool;
    }

    public abstract Task<DataTable> GetSamplesAsync();

    protected async Task<Arr<(ParameterState ParameterState, double[] Samples)>> DoRankCorrelationAsync(
      Arr<(ParameterState ParameterState, double[] Samples)> parameterSamples,
      IRVisClient rvisClient
      )
    {
      RequireTrue(_rankCorrelationDesign.RankCorrelationDesignType == RankCorrelationDesignType.ImanConn);
      RequireTrue(parameterSamples.ForAll(ps => ps.ParameterState.IsSelected));
      RequireTrue(parameterSamples.ForAll(ps => ps.ParameterState.DistributionType != DistributionType.Invariant));
      RequireTrue(parameterSamples.Count > 1);

      var parameters = parameterSamples.Map(ps => ps.ParameterState.Name);
      RequireTrue(parameters.ForAll(
        p => _rankCorrelationDesign.Correlations.Exists(c => c.Parameter == p)
        ));

      var samples = parameterSamples
        .Map(ps => ps.Samples)
        .ToArray()
        .Transpose();

      var objectNameSamples = "dvm_rc_ds";
      await rvisClient.CreateMatrixAsync(samples, objectNameSamples);

      var correlations = _rankCorrelationDesign.Correlations
        .CorrelationsFor(parameters)
        .CorrelationsToMatrix();

      var objectNameCorrelations = "dvm_rc_corr";
      await rvisClient.CreateMatrixAsync(correlations.ToJagged(), objectNameCorrelations);

      var objectNameCorrelated = "dvm_rc_dsc";
      var code = $"{objectNameCorrelated} <- mc2d::cornode({objectNameSamples}, target={objectNameCorrelations})";

      await rvisClient.EvaluateNonQueryAsync(code);

      var correlated = await rvisClient.EvaluateNumDataAsync(objectNameCorrelated);

      parameterSamples = parameterSamples
        .Map((i, ps) => (ps.ParameterState, correlated[i].Data.ToArray()))
        .ToArr();

      return parameterSamples;
    }

    protected DataTable MakeSamples(
      Arr<ParameterState> selectedParameters,
      Arr<(ParameterState ParameterState, double[] Samples)> parameterSamples
      )
    {
      var dataTable = new DataTable();

      selectedParameters.Iter(pb =>
      {
        dataTable.Columns.Add(
          new DataColumn(
            pb.Name,
            typeof(double)
            )
          );
      });

      var itemArray = Enumerable
        .Repeat(NaN, selectedParameters.Count)
        .Cast<object>()
        .ToArray();

      selectedParameters.Iter((i, ps) =>
      {
        if (ps.DistributionType != DistributionType.Invariant) return;
        var distribution = RequireInstanceOf<InvariantDistribution>(ps.GetDistribution());
        itemArray[i] = distribution.Value;
      });

      var targetIndices = itemArray
        .Cast<double>()
        .Select((d, i) => IsNaN(d) ? i : NOT_FOUND)
        .Where(i => i.IsFound())
        .ToArr();

      RequireTrue(targetIndices.Count == parameterSamples.Count);

      Range(0, _nSamples).Iter(i =>
      {
        targetIndices.Iter(
          (j, ti) => itemArray[ti] = parameterSamples[j].Samples[i]
          );

        dataTable.Rows.Add(itemArray);
      });

      dataTable.AcceptChanges();

      return dataTable;
    }

    protected readonly Arr<ParameterState> _parameterStates;
    protected readonly RankCorrelationDesign _rankCorrelationDesign;
    protected readonly int _nSamples;
    protected readonly int? _seed;
    protected readonly IRVisServerPool _serverPool;
  }
}
