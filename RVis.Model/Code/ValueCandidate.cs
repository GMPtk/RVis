using LanguageExt;
using RVis.Data;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace RVis.Model
{
  public class ValueCandidate
  {
    public static (ValueCandidate, Arr<ValueCandidate>) CreateForExec(NumDataTable table, Arr<ISymbolInfo> symbolInfos)
    {
      RequireTrue(table.NColumns > 1);
      RequireTrue(table.NumDataColumns[0].Length > 1);

      var valueCandidates = table.NumDataColumns
        .Map(ndc => new ValueCandidate(ndc, symbolInfos));

      var independentVariable = valueCandidates.Head();
      independentVariable.ElementCandidates[0].IsUsed = true;
      independentVariable.ElementCandidates[0].IsIndependentVariable = true;

      valueCandidates = valueCandidates
        .Tail()
        .OrderBy(vc => vc.Name);

      return (independentVariable, valueCandidates.ToArr());
    }

    private ValueCandidate(NumDataColumn column, Arr<ISymbolInfo> symbolInfos)
    {
      Name = column.Name;

      ElementCandidates = Array(
        new ElementCandidate(column.Name, column.Data.ToArr(), symbolInfos)
      );
    }

    public ValueCandidate(ISymbolInfo symbolInfo, Arr<ISymbolInfo> symbolInfos)
    {
      RequireNotNullEmptyWhiteSpace(symbolInfo.Symbol);
      RequireTrue(symbolInfo.Value?.NColumns > 0);

      SymbolInfo = symbolInfo;

      Name = symbolInfo.Symbol;

      ElementCandidates = symbolInfo.Value.NumDataColumns
        .Map(ndc => new ElementCandidate(ndc.Name, ndc.Data.ToArr(), symbolInfos))
        .OrderBy(ec => ec.Name)
        .ToArr();

      if (ElementCandidates.Count == 1)
      {
        var elementCandidate = ElementCandidates.Head();
        elementCandidate.Description ??= symbolInfo.Comment;
        elementCandidate.Unit ??= symbolInfo.Unit;
      }
    }

    public ISymbolInfo? SymbolInfo { get; }

    public string Name { get; }

    public Arr<ElementCandidate> ElementCandidates { get; }
  }
}
