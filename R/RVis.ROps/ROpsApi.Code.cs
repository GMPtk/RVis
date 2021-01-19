using RDotNet;
using RVis.Data;
using RVis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static RVis.Base.Check;
using static System.Linq.Enumerable;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    public static void SourceLines(string[] lines)
    {
      var instance = REngine.GetInstance();
      SourceLines(lines, instance);
    }

    public static void SourceFile(string pathToCode)
    {
      var instance = REngine.GetInstance();
      SourceFile(pathToCode, instance);
    }

    public static Dictionary<string, object[]?> Evaluate(string code)
    {
      var instance = REngine.GetInstance();

      var sexp = instance.Evaluate(code);

      RequireNotNull(sexp, "Code evaluation produced null output");

      _log.Debug($"Evaluating {code} produced a {sexp.Type}");

      return SexpToDictionary(sexp);
    }

    public static Dictionary<string, string[]?> EvaluateStrings(string code)
    {
      var instance = REngine.GetInstance();

      var sexp = instance.Evaluate(code);

      RequireNotNull(sexp, "Code evaluation produced null output");

      _log.Debug($"Evaluating {code} produced a {sexp.Type}");

      return SexpToStringDictionary(sexp);
    }

    public static Dictionary<string, double[]?> EvaluateDoubles(string code)
    {
      var instance = REngine.GetInstance();

      var sexp = instance.Evaluate(code);

      RequireNotNull(sexp, "Code evaluation produced null output");

      _log.Debug($"Evaluating {code} produced a {sexp.Type}");

      return SexpToDoubleDictionary(sexp);
    }

    public static NumDataColumn[] EvaluateNumData(string code)
    {
      var instance = REngine.GetInstance();

      var sexp = instance.Evaluate(code);

      RequireNotNull(sexp, "Code evaluation produced null output");

      _log.Debug($"Evaluating {code} produced a {sexp.Type}");

      var matrices = SexpToMatrices(sexp);

      if (0 == matrices.Length) return Array.Empty<NumDataColumn>();

      var numDataColumns = matrices.SelectMany(
        m => MatrixToNumDataColumns(m.Array, m.ColumnNames)
        );

      return numDataColumns.ToArray();
    }

    public static void EvaluateNonQuery(string code)
    {
      var instance = REngine.GetInstance();
      instance.Evaluate(code);
    }

    public static SymbolInfo[] InspectSymbols(string pathToCode)
    {
      var instance = REngine.GetInstance();

      SourceFile(pathToCode, instance);
      var symbolInfos = InspectGlobalEnvironment(instance);

      var lines = File.ReadAllLines(pathToCode);
      var codeSymbolInfos = InspectCode(lines, symbolInfos, instance);
      symbolInfos.AddRange(codeSymbolInfos);

      return symbolInfos.ToArray();
    }
  }
}
