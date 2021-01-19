using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RVis.Data
{
  [ProtoContract]
  public sealed class NumDataTable : DataTableBase
  {
    public static void SaveToBinaryFile(NumDataTable numDataTable, string path)
    {
      var fileStream = new FileStream(path, FileMode.Create);

      try
      {
        Serializer.Serialize(fileStream, numDataTable);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(
          $"Failed to save binary for {nameof(NumDataTable)} to {path}",
          nameof(path),
          ex
          );
      }
      finally
      {
        fileStream.Close();
      }
    }

    public static NumDataTable LoadFromBinaryFile(string path)
    {
      var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      try
      {
        return Serializer.Deserialize<NumDataTable>(fileStream);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(
          $"Failed to load binary for {nameof(NumDataTable)} from {path}",
          nameof(path),
          ex
          );
      }
      finally
      {
        fileStream.Close();
      }
    }

    public NumDataTable()
    {

    }

    public NumDataTable(string? name, IEnumerable<NumDataColumn> columns)
    {
      Check(columns);

      _name = name;
      _columns = columns.ToArray();
    }

    public NumDataTable(string? name, params NumDataColumn[] columns)
      : this(name, columns as IEnumerable<NumDataColumn>)
    {
    }

    [ProtoIgnore]
    public override string? Name => _name;

    [ProtoIgnore]
    public override IReadOnlyList<IDataColumn> DataColumns => NumDataColumns;

    [ProtoIgnore]
    public IReadOnlyList<NumDataColumn> NumDataColumns => _columns;

    public new NumDataColumn this[string name] => NumDataColumns.Single(c => c.Name == name);

    public new NumDataColumn this[int index] => NumDataColumns[index];

    [ProtoMember(1)]
    private readonly string? _name;

    [ProtoMember(2)]
    private readonly NumDataColumn[] _columns = null!;
  }
}
