using LanguageExt;
using RVis.Base.Extensions;
using static RVis.Base.Check;

namespace RVis.Model.Extensions
{
  public static class DistributionExt
  {
    public static string? ToStringIfSome(this Option<IDistribution> maybeDistribution, string variableName) =>
      maybeDistribution.MatchUnsafe<string?>(d => d.ToString(variableName), () => null);

    public static string ToString(this Option<IDistribution> maybeDistribution, string variableName) =>
      maybeDistribution.Match(d => d.ToString(variableName), () => DistributionType.None.ToString());

    public static Arr<IDistribution> SetDistribution(this Arr<IDistribution> distributions, IDistribution distribution)
    {
      var index = distributions.FindIndex(d => d.DistributionType == distribution.DistributionType);
      RequireTrue(index.IsFound());
      return distributions.SetItem(index, distribution);
    }

    public static bool IsInverseTransformSamplingType(this IDistribution distribution) =>
      DistributionType.InvTrfmSplngTypes.HasFlag(distribution.DistributionType);

    public static bool HasRQuantileSignature(this IDistribution distribution) =>
      DistributionType.RQuantileSignatureTypes.HasFlag(distribution.DistributionType);

    public static bool RequiresInverseTransformSampling(this IDistribution distribution) =>
      distribution.IsInverseTransformSamplingType() && (!distribution.HasRQuantileSignature() || distribution.IsTruncated);
  }
}
