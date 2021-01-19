using LanguageExt;
using RVis.Data;
using RVis.Model;
using RVisUI.AppInf;
using RVisUI.AppInf.Extensions;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static Sampling.Properties.Resources;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.String;
using DataTable = System.Data.DataTable;

namespace Sampling
{
  internal sealed class HypercubeSampleGenerator : SampleGeneratorBase
  {
    internal HypercubeSampleGenerator(
      Arr<ParameterState> parameterStates,
      LatinHypercubeDesign latinHypercubeDesign,
      RankCorrelationDesign rankCorrelationDesign,
      int nSamples,
      int? seed,
      IRVisServerPool serverPool
      ) : base(parameterStates, rankCorrelationDesign, nSamples, seed, serverPool)
    {
      _latinHypercubeDesign = latinHypercubeDesign;
    }

    public override async Task<DataTable> GetSamplesAsync()
    {
      using var serverLicense = _serverPool.RequestServer().Case switch
      {
        SomeCase<ServerLicense>(var sl) => sl,
        _ => throw new Exception("No R server available")
      };

      var selectedParameters = _parameterStates.Filter(ps => ps.IsSelected);

      var parameterBounds = selectedParameters
        .Filter(ps => ps.DistributionType != DistributionType.Invariant)
        .Map(ps =>
        {
          var distribution = ps.GetDistribution();

          if (distribution is UniformDistribution uniformDistribution)
          {
            return (ps.Name, uniformDistribution.Lower, uniformDistribution.Upper, ICDF: fun<double, double>(d => d));
          }

          var (lower, upper) = distribution.IsTruncated
            ? distribution.CumulativeDistributionAtBounds
            : (0d, 1d);

          return (ps.Name, lower, upper, d => distribution.InverseCumulativeDistribution(d));
        });

      if (parameterBounds.IsEmpty)
      {
        return MakeSamples(selectedParameters, default);
      }

      var n = _nSamples;
      var dimension = parameterBounds.Count;
      var randomized =
        _latinHypercubeDesign.LatinHypercubeDesignType == LatinHypercubeDesignType.Randomized
          ? "TRUE"
          : "FALSE";
      var seedValue = _seed?.ToString(InvariantCulture) ?? "NULL";

      var code = Format(
        InvariantCulture,
        FMT_LHSDESIGN,
        n,
        dimension,
        randomized,
        seedValue
        );

      var rClient = await serverLicense.GetRClientAsync();

      await rClient.EvaluateNonQueryAsync(code);

      var useSimulatedAnnealing = !IsNaN(_latinHypercubeDesign.T0);

      NumDataColumn[] design;

      if (useSimulatedAnnealing)
      {
        var t0 = _latinHypercubeDesign.T0;
        var c = _latinHypercubeDesign.C;
        var it = _latinHypercubeDesign.Iterations;
        var p = _latinHypercubeDesign.P;
        var profile = _latinHypercubeDesign.Profile switch
        {
          TemperatureDownProfile.Geometrical => "GEOM",
          TemperatureDownProfile.GeometricalMorris => "GEOM_MORRIS",
          TemperatureDownProfile.Linear => "LINEAR",
          _ => throw new ArgumentOutOfRangeException(nameof(TemperatureDownProfile))
        };
        var imax = _latinHypercubeDesign.Imax;

        code = Format(
          InvariantCulture,
          FMT_MAXIMINSALHS,
          t0,
          c,
          it,
          p,
          profile,
          imax
          );

        await rClient.EvaluateNonQueryAsync(code);
        design = await rClient.EvaluateNumDataAsync("rvis_lhsDesign_out_opt$design");
      }
      else
      {
        design = await rClient.EvaluateNumDataAsync("rvis_lhsDesign_out$design");
      }

      var parameterSamples = selectedParameters
        .Filter(ps => ps.DistributionType != DistributionType.Invariant)
        .Map((i, ps) =>
        {
          var (name, lower, upper, icdf) = parameterBounds[i];
          RequireTrue(name == ps.Name);

          var samples = design[i].Data
            .Select(d => icdf(lower + d * (upper - lower)))
            .ToArray();

          return (ParameterState: ps, Samples: samples);
        })
        .ToArr();

      var doRankCorrelation =
        _rankCorrelationDesign.RankCorrelationDesignType != RankCorrelationDesignType.None &&
        parameterSamples.Count > 1;

      if (doRankCorrelation)
      {
        parameterSamples = await DoRankCorrelationAsync(parameterSamples, rClient);
      }

      var samples = MakeSamples(selectedParameters, parameterSamples);

      return samples;
    }

    private readonly LatinHypercubeDesign _latinHypercubeDesign;
  }
}
