using System;
using System.Collections.Generic;
using System.Linq;

namespace Sampling
{
  internal class _RankCorrelationDesignDTO
  {
    public string? RankCorrelationDesignType { get; set; }

    public Dictionary<string, double[]>? Correlations { get; set; }
  }

  internal static class RCPersist
  {
    internal static RankCorrelationDesign FromDTO(this _RankCorrelationDesignDTO dto)
    {
      if (dto == default) return RankCorrelationDesign.Default;

      var rankCorrelationDesignType =
        Enum.TryParse(dto.RankCorrelationDesignType, out RankCorrelationDesignType rcdt)
          ? rcdt
          : RankCorrelationDesignType.None;

      var correlations = dto.Correlations?
        .Select(kvp => (Parameter: kvp.Key, Correlations: kvp.Value.ToArr()))
        .OrderBy(t => t.Parameter.ToUpperInvariant())
        .ToArr() ?? default;

      var nCorrelations = correlations.Count;

      var isValid = correlations
        .Map((i, c) => c.Correlations.Count == (nCorrelations - i - 1))
        .ForAll(b => b);

      if (!isValid)
      {
        Logger.Log.Error("Unexpected correlations configuration");
        return RankCorrelationDesign.Default;
      }

      return new RankCorrelationDesign(rankCorrelationDesignType, correlations);
    }

    internal static _RankCorrelationDesignDTO ToDTO(this RankCorrelationDesign rankCorrelationDesign)
    {
      return new _RankCorrelationDesignDTO
      {
        RankCorrelationDesignType = rankCorrelationDesign.RankCorrelationDesignType.ToString(),
        Correlations = rankCorrelationDesign.Correlations.ToDictionary(
          c => c.Parameter,
          c => c.Correlations.ToArray()
          )
      };
    }
  }
}
