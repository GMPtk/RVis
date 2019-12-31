using RVis.Model;

namespace Sampling
{
  internal readonly struct DesignParameter
  {
    internal DesignParameter(string name, IDistribution distribution)
    {
      Name = name;
      Distribution = distribution;
    }

    internal string Name { get; }
    internal IDistribution Distribution { get; }
  }
}
