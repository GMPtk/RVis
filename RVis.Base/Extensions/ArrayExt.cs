using System;
using System.Diagnostics.CodeAnalysis;
using static RVis.Base.Check;

namespace RVis.Base.Extensions
{
  public static class ArrayExt
  {
    public static T[][] Transpose<T>(this T[][] t)
    {
      if (t.Length == 0) return Array.Empty<T[]>();

      RequireTrue(t.AllSame(a => a.Length));

      var u = new T[t[0].Length][];

      for (int i = 0; i < t[0].Length; ++i)
      {
        u[i] = new T[t.Length];
      }

      for (int i = 0; i < t[0].Length; ++i)
      {
        for (int j = 0; j < t.Length; ++j)
        {
          u[i][j] = t[j][i];
        }
      }

      return u;
    }

    public static T[,] ToMultidimensional<T>(this T[][] t)
    {
      if (t.Length == 0) return new T[0, 0];

      RequireTrue(t.AllSame(a => a.Length));

      if (t[0].Length == 0) return new T[t.Length, 0];

      var u = new T[t.Length, t[0].Length];

      for (int i = 0; i < t.Length; ++i)
      {
        for (int j = 0; j < t[0].Length; ++j)
        {
          u[i, j] = t[i][j];
        }
      }

      return u;
    }

    public static T[][] ToJagged<T>(this T[,] t)
    {
      RequireTrue(t.GetLowerBound(0) == 0);
      RequireTrue(t.GetLowerBound(1) == 0);

      var u = new T[t.GetLength(0)][];

      for (int i = 0; i < t.GetLength(0); ++i)
      {
        u[i] = new T[t.GetLength(1)];

        for (int j = 0; j < t.GetLength(1); ++j)
        {
          u[i][j] = t[i, j];
        }
      }

      return u;
    }

    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this T[]? t) =>
      t == default || t.Length == 0;

    public static bool IsEmpty<T>(this T[] t) =>
      t.Length == 0;
  }
}
