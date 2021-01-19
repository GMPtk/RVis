using System.Collections.Generic;
using System.Linq;
using static RVis.Base.Check;

namespace RVis.Data
{
  public class DataColumn<T> : DataColumnBase<T>
  {
    public DataColumn(string name, IEnumerable<T> data)
    {
      RequireNotNull(data);

      _name = name;
      _data = data.ToArray();
    }

    public override string Name => _name;

    public override IReadOnlyList<T> Data => _data;

    internal string _name = null!;
    internal T[] _data = null!;

    protected DataColumn() { }
  }
}
