namespace RVis.Base.Extensions
{
  public static class ArrayExt
  {
    public static bool IsNullOrEmpty<T>(this T[] t) =>
      t == default || t.Length == 0;

    public static bool IsEmpty<T>(this T[] t) =>
      t.Length == 0;
  }
}
