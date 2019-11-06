using System;

namespace RVis.Base
{
  public static class Constant
  {
    #region Maths

    /// <summary>
    /// Number of digits in the mantissa of double precision floating point numbers.
    /// </summary>
    /// <remarks>Probably system dependent, but in this case taken from %lt;Visual Studio root&gt;\VC\include\float.h</remarks>
    /// <see cref="http://en.wikipedia.org/wiki/Float.h"/>
    public const int DBL_MANT_DIG = 53;

    /// <summary>
    /// The smallest number, x, s.t. 1.0 + x != 1.
    /// </summary>
    /// <remarks>Approx. 2.220446049250313e-16.</remarks>
    public static readonly double EPSILON = Math.Pow(2.0, 1.0 - DBL_MANT_DIG);

    /// <summary>
    /// Used for checking if two floating point numbers are pretty much the same. 
    /// </summary>
    /// <remarks>Approx. 1.490116119384765625e-08.</remarks>
    public static readonly double TOLERANCE = Math.Sqrt(EPSILON);

    #endregion
  }
}
