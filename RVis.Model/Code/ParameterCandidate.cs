using LanguageExt;
using RVis.Base.Extensions;
using System.Linq;
using System.Text.RegularExpressions;
using static RVis.Base.Check;

namespace RVis.Model
{
  public class ParameterCandidate
  {
    public static Arr<ParameterCandidate> CreateForExec(ISymbolInfo formal, Arr<ISymbolInfo> symbolInfos)
    {
      RequireTrue(formal.Value?.NColumns > 0);
      RequireTrue(formal.Value.NRows == 1);

      var parameterCandidates = formal.Value.NumDataColumns
        .Map(ndc => new ParameterCandidate(ndc.Name, ndc[0]))
        .OrderBy(pc => pc.Name)
        .ToArr();

      foreach (var parameterCandidate in parameterCandidates)
      {
        if (parameterCandidate.Description.IsntAString() || parameterCandidate.Unit.IsntAString())
        {
          var labelled = symbolInfos.Filter(si => si.Comment.IsAString() || si.Unit.IsAString());

          var symbolInfo = labelled.Find(si => si.Symbol == parameterCandidate.Name);

          if (symbolInfo.IsNone)
          {
            var re = new Regex($"^{parameterCandidate.Name}\\W*=[^=]");
            symbolInfo = labelled.Filter(si => si.Code.IsAString()).Find(si => re.IsMatch(si.Code!));
          }

          symbolInfo.IfSome(si =>
          {
            parameterCandidate.Description ??= si.Comment;
            parameterCandidate.Unit ??= si.Unit;
          });
        }
      }

      return parameterCandidates;
    }

    private ParameterCandidate(string name, double value)
    {
      Name = name;
      Value = value;
    }

    public ParameterCandidate(ISymbolInfo symbolInfo)
    {
      RequireNotNullEmptyWhiteSpace(symbolInfo.Symbol);
      RequireTrue(symbolInfo.Scalar.HasValue);

      SymbolInfo = symbolInfo;

      Name = symbolInfo.Symbol;
      Value = symbolInfo.Scalar.Value;
      Description = symbolInfo.Comment;
      Unit = symbolInfo.Unit;
    }

    public ISymbolInfo? SymbolInfo { get; private set; }

    public string Name { get; }

    public double Value { get; }

    public string? Description { get; set; }

    public string? Unit { get; set; }

    public bool IsUsed { get; set; }
  }
}
