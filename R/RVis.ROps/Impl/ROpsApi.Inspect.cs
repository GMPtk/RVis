using RDotNet;
using RDotNet.Internals;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static RVis.Base.Extensions.NumExt;
using static System.String;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    private static SymbolInfo SymbolicExpressionToSymbolInfo(
      string? name,
      SymbolicExpression symbolicExpression,
      SymbolType symbolType
      )
    {
      NumDataTable? value = default;
      int length = 0;
      string[]? names = default;

      try
      {
        var matrices = SexpToMatrices(symbolicExpression, name);
        var tabulatedData = new List<NumDataColumn>();
        foreach (var (columnNames, array) in matrices)
        {
          var numDataColumns = MatrixToNumDataColumns(
            array,
            columnNames
            );
          tabulatedData.AddRange(numDataColumns);
        }

        var isJagged = tabulatedData.NotAllSame(ndc => ndc.Length);
        if (isJagged)
        {
          // non-output so process as input (parameters)
          tabulatedData = tabulatedData.Where(ndc => ndc.Length == 1).ToList();
        }

        value = new NumDataTable(name, tabulatedData);
        length = value.NColumns;
        length = length > 0 ? length * value[0].Length : length;
        names = value.ColumnNames.ToArray();
      }
      catch (Exception) { }

      return new SymbolInfo
      {
        Symbol = name,
        Value = value,
        SymbolType = symbolType,
        SymbolicExpressionType = (int)symbolicExpression.Type,
        Length = length,
        Names = names
      };
    }

    private static SymbolType QuerySymbolicExpression(SymbolicExpression symbolicExpression)
    {
      if (symbolicExpression.IsDataFrame()) return SymbolType.DataFrame;
      if (symbolicExpression.IsFunction()) return SymbolType.Function;
      if (symbolicExpression.IsMatrix()) return SymbolType.Matrix;
      if (symbolicExpression.IsList()) return SymbolType.List;
      if (symbolicExpression.IsVector()) return SymbolType.Vector;

      return SymbolType.NonRVisType;
    }

    private static List<SymbolInfo> InspectGlobalEnvironment(REngine instance)
    {
      var symbolInfos = new List<SymbolInfo>();
      var globals = instance.GlobalEnvironment.AsList();

      var names = globals.Names;
      foreach (var name in names)
      {
        var symbolicExpression = globals[name];
        var symbolicExpressionType = symbolicExpression.Type;
        var symbolType = QuerySymbolicExpression(symbolicExpression);

        var isNumeric =
          symbolicExpressionType == SymbolicExpressionType.NumericVector ||
          symbolicExpressionType == SymbolicExpressionType.IntegerVector;

        if (symbolType == SymbolType.Function)
        {
          var length = instance.Evaluate($"length(formals({name}))").AsInteger()[0];
          symbolInfos.Add(new SymbolInfo
          {
            Symbol = name,
            SymbolicExpressionType = (int)symbolicExpressionType,
            SymbolType = symbolType,
            Length = length
          });
        }
        else if (symbolType == SymbolType.DataFrame || symbolType == SymbolType.List || isNumeric)
        {
          var symbolInfo = SymbolicExpressionToSymbolInfo(name, symbolicExpression, symbolType);
          symbolInfos.Add(symbolInfo);
        }
      }

      return symbolInfos;
    }

    private static (string Unit, string? Remainder) ExtractUnit(string text)
    {
      var indexClose = text.LastIndexOf(']');
      var indexOpen = NOT_FOUND;

      if (indexClose.IsFound())
      {
        indexOpen = text.LastIndexOf('[');
      }
      else
      {
        indexClose = text.LastIndexOf(')');

        if (indexClose.IsFound())
        {
          indexOpen = text.LastIndexOf('(');
        }
      }

      if (indexOpen.IsntFound()) return default;

      var unit = text.Substring(indexOpen + 1, indexClose - indexOpen - 1);
      var remainder = text.Remove(indexOpen, unit.Length + 2);

      unit = unit.Trim().RejectEmpty();

      if (unit == default) return default;

      return (unit, remainder.Trim().RejectEmpty());
    }

    private static (string?, string?) EnsureUnit(string? comment, string? unit)
    {
      var noData = comment == default && unit == default;
      if (noData) return default;

      var extractedDelimited = false;

      if (unit != default)
      {
        var unitExtraction = ExtractUnit(unit);
        extractedDelimited = unitExtraction != default;
        if (extractedDelimited)
        {
          unit = unitExtraction.Unit;

          if (comment == default)
          {
            comment = unitExtraction.Remainder;
          }
          else if (unitExtraction.Remainder != default)
          {
            comment = comment + "; " + unitExtraction.Remainder;
          }
        }
      }

      if (extractedDelimited) return (comment, unit);

      if (comment == default) return (unit, default);

      var commentExtraction = ExtractUnit(comment);

      extractedDelimited = commentExtraction != default;

      if (!extractedDelimited)
      {
        if (unit != default)
        {
          comment = comment + "; " + unit;
          unit = default;
        }

        return (comment, unit);
      }

      comment = unit;

      if (comment == default)
      {
        comment = commentExtraction.Remainder;
      }
      else if (commentExtraction.Remainder != default)
      {
        comment = comment + "; " + commentExtraction.Remainder;
      }

      return (comment, commentExtraction.Unit);
    }

    private static List<SymbolInfo> InspectCode(
      string[] lines,
      List<SymbolInfo> globalSymbolInfos,
      REngine instance
      )
    {
      var list = new List<SymbolInfo>();

      var level = 0; // keep track how far from global env we are

      for (var i = 0; i < lines.Length; ++i)
      {
        var line = lines[i].Trim();

        // disregard empty lines and comments
        if (0 == line.Length) continue;
        if (line.StartsWith("#", StringComparison.InvariantCulture)) continue;

        var levelShift = line.Sum(c => c == '{' ? 1 : c == '}' ? -1 : 0);
        level += levelShift;

        // don't inspect if entering or exiting a block
        if (0 != levelShift) continue;

        // strip out prototype's code marking devices
        line = _reProtoAnno.Replace(line, "#");

        // format supported is: [symbol <-] expression [# comment]
        // no =, no double #, no double <- ...

        var parts = line.Split(new[] { "<-" }, StringSplitOptions.None);

        if (2 < parts.Length) continue;

        string? symbol = default;
        string? code = default;
        string? comment = default;
        string? unit = default;
        double? scalar = default;
        NumDataTable? value = default;
        var length = 0;
        string[]? names = default;
        var symbolType = SymbolType.NonRVisType;
        var symbolicExpressionType = SymbolicExpressionType.Null;

        if (2 == parts.Length)
        {
          symbol = parts[0].Trim();
          code = parts[1].Trim();
        }
        else
        {
          code = parts[0].Trim();
        }

        parts = code.Split(new[] { "#" }, StringSplitOptions.None);

        if (2 < parts.Length)
        {
          parts = new[] { parts[0], Join(Empty, parts[1..]) };
        }

        if (2 == parts.Length)
        {
          code = parts[0].Trim();
          unit = parts[1].Trim();
        }

        if (double.TryParse(code, out double d)) scalar = d;

        if (i > 0)
        {
          var lineBefore = lines[i - 1].Trim();

          if (lineBefore.StartsWith("#", StringComparison.InvariantCulture))
          {
            comment = lineBefore[1..].Trim();
          }
        }

        (comment, unit) = EnsureUnit(comment.RejectEmpty(), unit.RejectEmpty());

        // if possible, populate global info
        if (level == 0)
        {
          var globalSymbolInfo = globalSymbolInfos.SingleOrDefault(
            si => si.Symbol == symbol && si.Code == default
            );

          if (globalSymbolInfo != default)
          {
            globalSymbolInfo.Code = code;
            globalSymbolInfo.Comment = comment;
            globalSymbolInfo.Unit = unit;
            globalSymbolInfo.Scalar = scalar;
            globalSymbolInfo.LineNo = i + 1;

            continue;
          }
        }

        code = code.TrimEnd(',', ';');
        try
        {
          var symbolicExpression = instance.Evaluate(code);
          symbolicExpressionType = symbolicExpression.Type;
          symbolType = QuerySymbolicExpression(symbolicExpression);

          var isNumeric =
            symbolicExpressionType == SymbolicExpressionType.NumericVector ||
            symbolicExpressionType == SymbolicExpressionType.IntegerVector;

          if (symbolType == SymbolType.DataFrame || symbolType == SymbolType.List || isNumeric)
          {
            var codeSymbolInfo = SymbolicExpressionToSymbolInfo(
              symbol,
              symbolicExpression,
              symbolType
              );

            value = codeSymbolInfo.Value;
            length = codeSymbolInfo.Length;
            names = codeSymbolInfo.Names;
          }
        }
        catch (Exception)
        { /* evaluating a line in isolation will lead to some errors :-) */ }

        var isUnidentifiedAndUnevaluated =
          comment == default &&
          unit == default &&
          symbolType == SymbolType.NonRVisType
          ;
        if (isUnidentifiedAndUnevaluated) continue;

        var symbolInfo = new SymbolInfo
        {
          Symbol = symbol,
          Code = code,
          Comment = comment,
          Unit = unit,
          Level = level,
          LineNo = i + 1,
          Scalar = scalar,
          Value = value,
          Length = length,
          Names = names,
          SymbolType = symbolType,
        };

        list.Add(symbolInfo);
      }

      return list;
    }

    private static readonly Regex _reProtoAnno = new Regex("#@[prio]");
  }
}
