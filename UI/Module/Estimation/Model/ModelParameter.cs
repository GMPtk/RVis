using RVis.Model;
using static System.Double;

namespace Estimation
{
  internal readonly struct ModelParameter
  {
    internal ModelParameter(
      string name, 
      IDistribution distribution,
      double value = NaN,
      double step = NaN
      )
    {
      Name = name;
      Distribution = distribution;
      Value = value;
      Step = IsNaN(step) ? distribution.Variance : step;
    }

    internal string Name { get; }
    internal IDistribution Distribution { get; }
    internal double Value { get; }
    internal double Step { get; }

    public ModelParameter GetProposal() => 
      new ModelParameter(
        Name, 
        Distribution, 
        Distribution.GetProposal(Value, Step), 
        Step
        );

    public ModelParameter ApplyBias(double bias) =>
      new ModelParameter(
        Name, 
        Distribution, 
        Value, 
        Distribution.ApplyBias(Step, bias)
        );

    public double GetValueDensity() => 
      Distribution.GetDensity(Value);
  }
}
