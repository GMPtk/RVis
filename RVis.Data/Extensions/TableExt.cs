using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using static RVis.Base.Check;

namespace RVis.Data.Extensions
{
  public static class TableExt
  {
    public static IDataColumn ToDataColumn(this Array data, string name)
    {
      var elementType = data.GetType().GetElementType();
      var typedColumn = typeof(DataColumn<>).MakeGenericType(elementType);
      var instance = Activator.CreateInstance(typedColumn, new object[] { name, data });
      return (IDataColumn)instance;
    }

    public static IDataColumn<T> ToDataColumn<T>(this T[] data, string name) => new DataColumn<T>(name, data);

    public static IDataColumn<T> ToDataColumn<T>(this IEnumerable<T> data, string name) => new DataColumn<T>(name, data);

    public static NumDataColumn ToDataColumn(this double[] data, string name) => new NumDataColumn(name, data);

    public static NumDataColumn ToDataColumn(this IEnumerable<double> data, string name) => new NumDataColumn(name, data);

    public static IDataColumn<T> Get<T>(this IDataTable dataTable, string name) => EnsureDataType<T>(dataTable[name]);

    public static IDataColumn<T> Get<T>(this IDataTable dataTable, int index) => EnsureDataType<T>(dataTable[index]);

    public static NumDataColumn GetIndependentVariable(this NumDataTable dataTable)
    {
      RequireTrue(dataTable.NColumns > 0, $"{nameof(GetIndependentVariable)} called with empty DataTable");
      if (!(dataTable[0] is NumDataColumn dataColumn)) throw new ArgumentException($"{nameof(GetIndependentVariable)} called against non-numeric IV", nameof(dataTable));
      return dataColumn;
    }

    public static Arr<NumDataColumn> GetDependentVariables(this NumDataTable dataTable)
    {
      return dataTable.NumDataColumns.Skip(1).ToArr();
    }

    private static IDataColumn<T> EnsureDataType<T>(IDataColumn dataColumn) =>
      dataColumn is IDataColumn<T> dt ?
        dt :
        throw new ArgumentException($"Requested column of type {typeof(T).Name}; runtime type is {dataColumn.GetDataType().Name}", nameof(dataColumn));
  }
}
