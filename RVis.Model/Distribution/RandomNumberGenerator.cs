using MathNet.Numerics.Random;
using System;

namespace RVis.Model
{
  public static class RandomNumberGenerator
  {
    public static Random Generator { get; private set; } = SystemRandomSource.Default;

    public static void ResetGenerator(int? seed = default) => Generator = seed.HasValue 
      ? new SystemRandomSource(seed.Value, true) 
      : SystemRandomSource.Default;
  }
}
