using ProtoBuf;
using RVis.Base.Extensions;
using System.Collections.Generic;
using System.Linq;
using static RVis.Base.Check;

namespace RVis.Data
{
  [ProtoContract]
  public abstract class DataTableBase : IDataTable
  {
    public abstract string? Name { get; }

    public abstract IReadOnlyList<IDataColumn> DataColumns { get; }

    public IReadOnlyList<string> ColumnNames => DataColumns.Select(dc => dc.Name).ToArray();

    public int NColumns => DataColumns.Count;

    public int NRows => 0 == NColumns ? 0 : DataColumns[0].Length;

    public bool HasColumn(string name) => DataColumns.Count(c => c.Name == name) == 1;

    public IDataColumn? this[string name] => DataColumns.SingleOrDefault(c => c.Name == name);

    public IDataColumn this[int index] => DataColumns[index];

    protected static void Check(IEnumerable<IDataColumn> columns)
    {
      RequireNotNull(columns);
      RequireTrue(!columns.Any() || columns.AllSame(dc => dc.Length));
    }
  }
}
