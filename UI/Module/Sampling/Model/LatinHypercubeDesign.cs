namespace Sampling
{
  internal readonly struct LatinHypercubeDesign
  {
    internal static readonly LatinHypercubeDesign Default = new LatinHypercubeDesign(
      LatinHypercubeDesignType.None,
      10d,
      0.99,
      200,
      50d,
      TemperatureDownProfile.Geometrical,
      100
      );

    internal LatinHypercubeDesign(
      LatinHypercubeDesignType latinHypercubeDesignType,
      double t0,
      double c,
      int iterations,
      double p,
      TemperatureDownProfile profile,
      int imax
      )
    {
      LatinHypercubeDesignType = latinHypercubeDesignType;
      T0 = t0;
      C = c;
      Iterations = iterations;
      P = p;
      Profile = profile;
      Imax = imax;
    }

    internal LatinHypercubeDesignType LatinHypercubeDesignType { get; }

    internal double T0 { get; }
    internal double C { get; }
    internal int Iterations { get; }
    internal double P { get; }
    internal TemperatureDownProfile Profile { get; }
    internal int Imax { get; }
  }
}
