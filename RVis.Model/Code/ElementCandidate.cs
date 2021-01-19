using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Text.RegularExpressions;
using static RVis.Base.Check;

namespace RVis.Model
{
  public class ElementCandidate
  {
    public ElementCandidate(string name, Arr<double> values, Arr<ISymbolInfo> symbolInfos)
    {
      RequireNotNullEmptyWhiteSpace(name);
      RequireTrue(values.Count > 1);

      Name = name;
      Values = values;

      AssignDefaultUnitDescription(symbolInfos);
    }

    public string Name { get; }

    public Arr<double> Values { get; }

    public string? Description { get; set; }

    public string? Unit { get; set; }

    public bool IsUsed { get; set; }

    public bool IsIndependentVariable { get; set; }

    private void AssignDefaultUnitDescription(Arr<ISymbolInfo> symbolInfos)
    {
      if (Description.IsntAString() || Unit.IsntAString())
      {
        var reversed = symbolInfos
          .Filter(si => si.Comment.IsAString() || si.Unit.IsAString())
          .Rev();

        var symbolInfo = reversed.Find(si => si.Symbol == Name);

        if (symbolInfo.IsNone)
        {
          // not found so look for symbol with single character prefix e.g. Rsymbol or dsymbol
          symbolInfo = reversed.Find(
            si => si.Symbol?.Length == Name.Length + 1 && si.Symbol.EndsWith(Name, StringComparison.InvariantCulture)
            );
        }

        if (symbolInfo.IsNone)
        {
          var re = new Regex($"^\\w?{Name}\\W*=[^=]");
          symbolInfo = reversed.Filter(si => si.Code.IsAString()).Find(si => re.IsMatch(si.Code!));
        }

        symbolInfo.IfSome(si =>
        {
          Description ??= si.Comment;
          Unit ??= si.Unit;
        });
      }
    }
  }
}
