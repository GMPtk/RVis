using RVis.Base.Extensions;
using System;

namespace Sampling
{
  internal class _LatinHypercubeDesignDTO
  {
    public string? LatinHypercubeDesignType { get; set; }

    public double? T0 { get; set; }
    public double? C { get; set; }
    public int Iterations { get; set; }
    public double? P { get; set; }
    public string? Profile { get; set; }
    public int Imax { get; set; }
  }

  internal static class LHDPersist
  {
    internal static LatinHypercubeDesign FromDTO(this _LatinHypercubeDesignDTO dto)
    {
      if (dto == default) return LatinHypercubeDesign.Default;

      return new LatinHypercubeDesign(
        Enum.TryParse(dto.LatinHypercubeDesignType, out LatinHypercubeDesignType latinHypercubeDesignType)
          ? latinHypercubeDesignType
          : LatinHypercubeDesignType.None,
        dto.T0.FromNullable(),
        dto.C.FromNullable(),
        dto.Iterations,
        dto.P.FromNullable(),
        Enum.TryParse(dto.Profile, out TemperatureDownProfile profile)
          ? profile
          : TemperatureDownProfile.Geometrical,
        dto.Imax
        );
    }

    internal static _LatinHypercubeDesignDTO ToDTO(this LatinHypercubeDesign latinHypercubeDesign)
    {
      return new _LatinHypercubeDesignDTO
      {
        C = latinHypercubeDesign.C.ToNullable(),
        Imax = latinHypercubeDesign.Imax,
        Iterations = latinHypercubeDesign.Iterations,
        LatinHypercubeDesignType = latinHypercubeDesign.LatinHypercubeDesignType.ToString(),
        P = latinHypercubeDesign.P.ToNullable(),
        Profile = latinHypercubeDesign.Profile.ToString(),
        T0 = latinHypercubeDesign.T0.ToNullable()
      };
    }
  }
}
