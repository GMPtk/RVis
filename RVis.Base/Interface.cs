namespace RVis.Base
{
  public interface IDeepCloneable
  {
    object DeepClone();
  }

  public interface IDeepCloneable<T> : IDeepCloneable
  {
    new T DeepClone();
  }
}
