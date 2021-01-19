using System.Diagnostics.CodeAnalysis;

namespace RVis.Base.Extensions
{
  public static class ObjExt
  {
    public static bool Resolve<T>(this object? o, [NotNullWhen(true)][MaybeNull] out T t)
    {
      if (o is not T u || u is null)
      {
        t = default;
        return false;
      }

      t = u;
      return true;
    }
  }
}
