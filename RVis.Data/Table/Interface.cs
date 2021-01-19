using System;
using System.Collections.Generic;

namespace RVis.Data
{
  public interface IDataTable
  {
    IDataColumn this[int index] { get; }
    IDataColumn? this[string name] { get; }
    IReadOnlyList<string> ColumnNames { get; }
    IReadOnlyList<IDataColumn> DataColumns { get; }
    string? Name { get; }
    int NColumns { get; }
    int NRows { get; }
    bool HasColumn(string name);
  }

  public interface IDataColumn
  {
    string Name { get; }
    int Length { get; }
    object? this[int row] { get; }
    IReadOnlyList<object> Data { get; }
    Type GetDataType();
  }

  public interface IDataColumn<T> : IDataColumn
  {
    new T this[int row] { get; }
    new IReadOnlyList<T> Data { get; }
  }
}
