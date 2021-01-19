using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RVis.Data
{
  [ProtoContract]
  public abstract class DataColumnBase<T> : IDataColumn<T>
  {
    public abstract string Name { get; }

    public int Length => Data.Count;

    public Type GetDataType() => typeof(T);

    public T this[int row] => Data[row];

    public abstract IReadOnlyList<T> Data { get; }

    object? IDataColumn.this[int row] => Data[row];

    IReadOnlyList<object> IDataColumn.Data => Data.Cast<object>().ToArray();
  }
}
