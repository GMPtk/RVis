using System;
using System.Collections.Generic;
using System.Linq;

namespace RVis.Base.Extensions
{
  public static class TupleExt
  {
    public static U Snd<T, U>(this IEnumerable<(T, U)> list, T t) =>
      list.Single(p => EqualityComparer<T>.Default.Equals(p.Item1, t)).Item2;

    public static bool IsEmpty<T, U>(this ValueTuple<T, U> tuple) =>
      EqualityComparer<T>.Default.Equals(tuple.Item1, default) &&
      EqualityComparer<U>.Default.Equals(tuple.Item2, default);

    public static bool IsntEmpty<T, U>(this ValueTuple<T, U> tuple) =>
      !IsEmpty(tuple);

    public static bool IsEmpty<T, U, V>(this ValueTuple<T, U, V> tuple) =>
      EqualityComparer<T>.Default.Equals(tuple.Item1, default) &&
      EqualityComparer<U>.Default.Equals(tuple.Item2, default) &&
      EqualityComparer<V>.Default.Equals(tuple.Item3, default);

    public static bool IsntEmpty<T, U, V>(this ValueTuple<T, U, V> tuple) =>
      !IsEmpty(tuple);

    public static bool IsEmpty<T, U, V, W>(this ValueTuple<T, U, V, W> tuple) =>
      EqualityComparer<T>.Default.Equals(tuple.Item1, default) &&
      EqualityComparer<U>.Default.Equals(tuple.Item2, default) &&
      EqualityComparer<V>.Default.Equals(tuple.Item3, default) &&
      EqualityComparer<W>.Default.Equals(tuple.Item4, default);

    public static bool IsntEmpty<T, U, V, W>(this ValueTuple<T, U, V, W> tuple) =>
      !IsEmpty(tuple);
  }
}
