namespace Sensitivity
{
  internal enum SensitivityMethod
  {
    Morris,
    Fast99
  }

  internal enum Fast99MeasureType
  {
    None,
    MainEffect,
    TotalEffect,
    Variance
  }

  internal enum MorrisMeasureType
  {
    None,
    Mu,
    MuStar,
    Sigma
  }
}
