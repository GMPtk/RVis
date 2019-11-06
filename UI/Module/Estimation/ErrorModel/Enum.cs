using System;

namespace Estimation
{
  [Flags]
  internal enum ErrorModelType
  {
    None =                        0b_00000000_00000000,
    Normal =                      0b_00000000_00000001,
    LogNormal =                   0b_00000000_00000010,
    HeteroscedasticPower =        0b_00000000_00000100,
    HeteroscedasticExp =          0b_00000000_00001000,

    All = 
      Normal | 
      LogNormal | 
      HeteroscedasticPower | 
      HeteroscedasticExp
  }
}
