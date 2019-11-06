namespace RVis.Base.Extensions
{
  public static class ObjExt
  {
    public static bool Resolve<T>(this object o, out T t)
    {
      if (!(o is T u))
      {
        t = default;
        return false;
      }

      t = u;
      return true;
    }
  }
}
