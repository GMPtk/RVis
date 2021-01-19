using ProtoBuf;
using System.Collections.Generic;
using System.Linq;
using static RVis.Base.Check;

namespace RVis.Data
{
  [ProtoContract]
  public sealed class NumDataColumn : DataColumnBase<double>
  {
    public NumDataColumn()
    {
    }

    public NumDataColumn(string name, IEnumerable<double> data)
    {
      RequireNotNull(data);

      _name = name;
      _data = data.ToArray();
    }

    [ProtoIgnore]
    public override string Name => _name;

    [ProtoIgnore]
    public override IReadOnlyList<double> Data => _data;

    [ProtoMember(1)]
    private readonly string _name = null!;

    [ProtoMember(2)]
    private readonly double[] _data = null!;
  }
}
