using System;
using System.Collections.Generic;
using System.Linq;

namespace RVis.Data
{
  public sealed class DataTable : DataTableBase
  {
    public DataTable(string name, IEnumerable<IDataColumn> columns)
    {
      Check(columns);

      Name = name;
      DataColumns = Array.AsReadOnly(columns.ToArray());
    }

    public DataTable(string name, params IDataColumn[] columns) 
      : this(name, columns as IEnumerable<IDataColumn>)
    {
    }

    public override string Name { get; }

    public override IReadOnlyList<IDataColumn> DataColumns { get; }

    private DataTable() 
    {
      Name = null!;
      DataColumns = null!;
    }
  }
}
