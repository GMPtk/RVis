using RVis.Data;

namespace RVis.Model
{
  public interface ISymbolInfo
  {
    string? Code { get; }
    string? Comment { get; }
    int Length { get; }
    int Level { get; }
    int LineNo { get; }
    string? Symbol { get; }
    string[]? Names { get; }
    double? Scalar { get; }
    SymbolType SymbolType { get; }
    int SymbolicExpressionType { get; }
    string? Unit { get; }
    NumDataTable? Value { get; }
  }
}
