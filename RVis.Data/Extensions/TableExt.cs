using LanguageExt;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static RVis.Base.Check;
using FxDataTable = System.Data.DataTable;

namespace RVis.Data.Extensions
{
  public static class TableExt
  {
    public static FxDataTable ToFxDataTable(this NumDataTable rvDataTable)
    {
      var fxDataTable = new FxDataTable(rvDataTable.Name);
      
      rvDataTable.ColumnNames.Iter(
        cn => fxDataTable.Columns.Add(cn, typeof(double))
        );
      
      for (var i = 0; i < rvDataTable.NRows; ++i)
      {
        var row = rvDataTable.GetRow<object>(i);
        fxDataTable.Rows.Add(row);
      }

      return fxDataTable;
    }

    public static T[] GetRow<T>(this IDataTable dataTable, int rowIndex)
    {
      RequireTrue(rowIndex < dataTable.NRows);

      var data = dataTable.DataColumns
        .Select(dc => Convert.ChangeType(dc[rowIndex], typeof(T)))
        .Cast<T>();

      return data.ToArray();
    }

    public static IDataColumn ToDataColumn(this Array data, string name)
    {
      var elementType = data.GetType().GetElementType();
      RequireNotNull(elementType);
      var typedColumn = typeof(DataColumn<>).MakeGenericType(elementType);
      var instance = Activator.CreateInstance(typedColumn, new object[] { name, data });
      RequireNotNull(instance);
      return (IDataColumn)instance;
    }

    public static IDataColumn<T> ToDataColumn<T>(this T[] data, string name) => new DataColumn<T>(name, data);

    public static IDataColumn<T> ToDataColumn<T>(this IEnumerable<T> data, string name) => new DataColumn<T>(name, data);

    public static NumDataColumn ToDataColumn(this double[] data, string name) => new NumDataColumn(name, data);

    public static NumDataColumn ToDataColumn(this IEnumerable<double> data, string name) => new NumDataColumn(name, data);

    public static IDataColumn<T> Get<T>(this IDataTable dataTable, string name) => EnsureDataType<T>(dataTable[name]);

    public static IDataColumn<T> Get<T>(this IDataTable dataTable, int index) => EnsureDataType<T>(dataTable[index]);

    private static IDataColumn<T> EnsureDataType<T>([NotNull] IDataColumn? dataColumn) =>
      dataColumn is IDataColumn<T> dt ?
        dt :
        throw new ArgumentException($"Requested column of type {typeof(T).Name}; runtime type is {dataColumn?.GetDataType().Name}", nameof(dataColumn));
  }
}
