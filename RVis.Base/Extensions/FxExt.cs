using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Reflection;
using static RVis.Base.Check;

namespace RVis.Base.Extensions
{
  public static class FxExt
  {
    public static bool HasNoSchema(this DataTable dataTable) =>
      dataTable.Columns.Count == 0;

    public static bool IsEmpty(this DataTable dataTable) =>
      dataTable.Rows.Count == 0;

    public static string GetDirectory(this Assembly assembly)
    {
      RequireNotNull(assembly);

      var uri = new UriBuilder(assembly.Location);
      var path = Uri.UnescapeDataString(uri.Path);
      var directory = Path.GetDirectoryName(path);
      RequireDirectory(directory);
      return directory;
    }

    public static string AsMmbrString(this Version version) =>
      $"{version.Major}.{version.Minor}.{version.Build:00000}.{version.Revision}";

    public static bool IsGreaterThan<T>(this T lhs, T rhs) =>
      Comparer<T>.Default.Compare(lhs, rhs) > 0;

    public static bool IsGreaterThanOrEqualTo<T>(this T lhs, T rhs) =>
      Comparer<T>.Default.Compare(lhs, rhs) >= 0;

    public static bool IsLessThan<T>(this T lhs, T rhs) =>
      Comparer<T>.Default.Compare(lhs, rhs) < 0;

    public static bool IsLessThanOrEqualTo<T>(this T lhs, T rhs) =>
      Comparer<T>.Default.Compare(lhs, rhs) <= 0;

    public static void InsertInOrdered<T, U>(this IList<T> list, T t, Func<T, U> keyLookup)
    {
      RequireNotNull(list);
      RequireNotNull(keyLookup);

      if (list.IsEmpty())
      {
        list.Add(t);
      }
      else
      {
        var index = 0;

        var sortKey = keyLookup(t);

        while (index < list.Count && keyLookup(list[index]).IsLessThan(sortKey))
        {
          ++index;
        }

        list.Insert(index, t);
      }
    }
  }
}
