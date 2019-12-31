using LanguageExt;
using RVis.Model;
using RVisUI.AppInf;
using RVisUI.AppInf.Extensions;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static RVis.Base.Check;
using DataTable = System.Data.DataTable;

namespace Sampling
{
  internal sealed class DistributionSampleGenerator : SampleGeneratorBase
  {
    internal DistributionSampleGenerator(
      Arr<ParameterState> parameterStates,
      RankCorrelationDesign rankCorrelationDesign,
      int nSamples,
      int? seed,
      IRVisServerPool serverPool
      ) : base(parameterStates, rankCorrelationDesign, nSamples, seed, serverPool)
    {
    }

    public override async Task<DataTable> GetSamplesAsync() =>
      await Task.Run(GetSamples);

    private DataTable GetSamples()
    {
      RandomNumberGenerator.ResetGenerator(_seed);

      var selectedParameters = _parameterStates.Filter(ps => ps.IsSelected);

      var parameterSamples = selectedParameters
        .Filter(ps => ps.DistributionType != DistributionType.Invariant)
        .OrderBy(ps => ps.Name.ToUpperInvariant())
        .Select(ps => (ParameterState: ps, Samples: new double[_nSamples]))
        .ToArr();

      parameterSamples.Iter(t =>
      {
        var distribution = t.ParameterState.GetDistribution();
        RequireTrue(distribution.IsConfigured);
        distribution.FillSamples(t.Samples);
      });

      var doRankCorrelation =
        _rankCorrelationDesign.RankCorrelationDesignType != RankCorrelationDesignType.None &&
        parameterSamples.Count > 1;

      if (doRankCorrelation)
      {
        using var serverLicense = _serverPool.RequestServer().Case switch
        {
          SomeCase<ServerLicense>(var sl) => sl,
          _ => throw new Exception("No R server available")
        };

        parameterSamples = DoRankCorrelation(parameterSamples, serverLicense.Client);
      }

      var samples = MakeSamples(selectedParameters, parameterSamples);

      return samples;
    }
  }
}
