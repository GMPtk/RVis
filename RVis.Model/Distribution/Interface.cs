using LanguageExt;

namespace RVis.Model
{
  public interface IDistribution
  {
    DistributionType DistributionType { get; }
    bool CanTruncate { get; }
    bool IsTruncated { get; }
    IDistribution WithLowerUpper(double lower, double upper);
    bool IsConfigured { get; }
    double Mean { get; }
    double Variance { get; }
    (double LowerP, double UpperP) CumulativeDistributionAtBounds { get; }
    void FillSamples(double[] samples);
    double GetSample();
    double GetProposal(double value, double step);
    double ApplyBias(double step, double bias);
    double InverseCumulativeDistribution(double p);
    (string? FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RQuantileSignature { get; }
    (string FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RInverseTransformSamplingSignature { get; }
    (Arr<double> Values, Arr<double> Densities) GetDensities(double lowerP, double upperP, int nDensities);
    double GetDensity(double value);
    string ToString();
    string ToString(string variableName);
  }

  public interface IDistribution<T> : IDistribution where T : class
  {
    T? Implementation { get; }
  }
}
