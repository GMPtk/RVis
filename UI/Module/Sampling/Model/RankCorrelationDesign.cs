using LanguageExt;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace Sampling
{
  internal readonly struct RankCorrelationDesign
  {
    internal static readonly RankCorrelationDesign Default = new RankCorrelationDesign();

    internal RankCorrelationDesign(
      RankCorrelationDesignType rankCorrelationDesignType,
      Arr<(string Parameter, Arr<double> Correlations)> correlations
      )
    {
      Range(0, correlations.Count)
      .Iter(
        i => RequireEqual(i, correlations[correlations.Count - i - 1].Correlations.Count)
        );

      RankCorrelationDesignType = rankCorrelationDesignType;
      Correlations = correlations;
    }

    internal RankCorrelationDesignType RankCorrelationDesignType { get; }

    internal Arr<(string Parameter, Arr<double> Correlations)> Correlations { get; }

    internal RankCorrelationDesign With(RankCorrelationDesignType rankCorrelationDesignType) =>
      new RankCorrelationDesign(rankCorrelationDesignType, Correlations);
  }
}
