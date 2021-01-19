using LanguageExt;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;

namespace RVis.Base.Extensions
{
  public static class CollExt
  {
    public static int FindIndex<T>(this Arr<T> source, Predicate<T> match) =>
      FindIndex(source, 0, match);

    public static int FindIndex<T>(this IReadOnlyList<T> source, Predicate<T> match) =>
      FindIndex(source, 0, match);

    public static int FindIndex<T>(this IReadOnlyList<T> source, int startIndex, Predicate<T> match)
    {
      RequireTrue(startIndex >= 0);

      for (var i = startIndex; i < source.Count; ++i)
      {
        if (match(source[i])) return i;
      }

      return NOT_FOUND;
    }

    public static bool IsCollection<T>([NotNullWhen(true)] this ICollection<T>? c) => c?.Any() == true;

    public static bool IsEmpty<T>(this ICollection<T> c) => !c.Any();

    public static void RemoveIf<T>(this IList<T> l, Func<T, bool> predicate)
    {
      var toRemove = l.Where(predicate).ToArray();
      foreach (var t in toRemove) l.Remove(t);
    }

    public static int MaxIndex<T>(this IEnumerable<T> sequence) where T : IComparable<T>
    {
      var maxIndex = NOT_FOUND;
      var maxValue = default(T);
      var index = 0;

      foreach (var value in sequence)
      {
        if (value.CompareTo(maxValue) > 0 || maxIndex.IsntFound())
        {
          maxIndex = index;
          maxValue = value;
        }
        ++index;
      }

      return maxIndex;
    }

    public static int MinIndex<T>(this IEnumerable<T> sequence) where T : IComparable<T>
    {
      var minIndex = NOT_FOUND;
      var minValue = default(T);
      var index = 0;

      foreach (var value in sequence)
      {
        if (value.CompareTo(minValue) < 0 || minIndex.IsntFound())
        {
          minIndex = index;
          minValue = value;
        }
        ++index;
      }

      return minIndex;
    }

    public static bool AllSame<T, U>(this IEnumerable<T> sequence, Func<T, U> likeness) =>
      sequence.GroupBy(likeness).Count() == 1;

    public static bool NotAllSame<T, U>(this IEnumerable<T> sequence, Func<T, U> likeness) =>
      !AllSame(sequence, likeness);

    public static bool AllUnique<T, U>(this IEnumerable<T> sequence, Func<T, U> discriminator) =>
      sequence.GroupBy(discriminator).All(g => g.Count() == 1);

    public static bool NotAllUnique<T, U>(this IEnumerable<T> sequence, Func<T, U> discriminator) =>
      !AllUnique(sequence, discriminator);

    public static void Add<T, U>(this IDictionary<T, U> dictionary, (T, U) item) =>
      dictionary.Add(item.Item1, item.Item2);

    public static void AddRange<T, U>(this IDictionary<T, U> dictionary, IEnumerable<(T, U)> items) =>
      items.Iter(dictionary.Add);
  }
}
