using ProtoBuf;
using RVis.Data;
using System;

namespace RVis.Model
{
  [Serializable]
  [ProtoContract]
  public class SymbolInfo : ISymbolInfo
  {
    [ProtoMember(1)]
    public string? Symbol { get; set; }

    [ProtoMember(2)]
    public NumDataTable? Value { get; set; }

    [ProtoMember(3)]
    public int Length { get; set; }

    [ProtoMember(4)]
    public string[]? Names { get; set; }

    [ProtoMember(5)]
    public SymbolType SymbolType { get; set; }

    [ProtoMember(6)]
    public int SymbolicExpressionType { get; set; }

    [ProtoMember(7)]
    public string? Code { get; set; }

    [ProtoMember(8)]
    public string? Comment { get; set; }

    [ProtoMember(9)]
    public string? Unit { get; set; }

    [ProtoMember(10)]
    public int Level { get; set; }

    [ProtoMember(11)]
    public int LineNo { get; set; }

    [ProtoMember(12)]
    public double? Scalar { get; set; }
  }
}
