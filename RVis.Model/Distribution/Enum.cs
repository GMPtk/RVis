using System;

namespace RVis.Model
{
  [Flags]
  public enum DistributionType
  {
    None          = 0b_00000000_00000000,
    Invariant     = 0b_00000000_00000001,
    Normal        = 0b_00000000_00000010,
    LogNormal     = 0b_00000000_00000100,
    Uniform       = 0b_00000000_00001000,
    LogUniform    = 0b_00000000_00010000,
    Beta          = 0b_00000000_00100000,
    BetaScaled    = 0b_00000000_01000000,
    Gamma         = 0b_00000000_10000000,
    InverseGamma  = 0b_00000001_00000000,
    StudentT      = 0b_00000010_00000000,

    All = 
        Invariant 
      | Normal 
      | LogNormal 
      | Uniform 
      | LogUniform 
      | Beta 
      | BetaScaled 
      | Gamma 
      | InverseGamma 
      | StudentT,

    RQuantileSignatureTypes =
        Beta
      | Gamma
      | LogNormal
      | Normal
      | Uniform,      

    InvTrfmSplngTypes =
        Beta
      | BetaScaled
      | Gamma
      | LogNormal
      | LogUniform
      | Normal
      | StudentT
      | Uniform
  }
}
