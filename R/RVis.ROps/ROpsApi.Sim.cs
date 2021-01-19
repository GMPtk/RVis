using RDotNet;
using RDotNet.Internals;
using RVis.Data;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    public static void RunExec(string pathToCode, SimCode code, SimInput input)
    {
      var sourceFile = new FileInfo(pathToCode);

      var firstRun =
        !string.Equals(_pathToLastSourcedFile, sourceFile.FullName, StringComparison.InvariantCultureIgnoreCase) ||
        sourceFile.LastWriteTimeUtc != _lastSourcedFileLastWriteTime;

      var instance = REngine.GetInstance();

      if (!firstRun)
      {
        var symbolNames = instance.GlobalEnvironment.GetSymbolNames();
        firstRun =
          !symbolNames.Contains(code.Exec) ||
          !symbolNames.Contains(code.Formal);
      }

      if (firstRun)
      {
        var clearOutput = $"if(exists(\"{ _execOutputName}\")) rm({_execOutputName})";
        instance.Evaluate(clearOutput);

        SourceFile(sourceFile.FullName, instance);

        var symbolNames = instance.GlobalEnvironment.GetSymbolNames();
        RequireFalse(
          symbolNames.Contains(_execOutputName),
          "Script contains reserved symbol: " + _execOutputName
          );
        RequireTrue(
          symbolNames.Contains(code.Exec),
          "Failed to find exec function: " + code.Exec
          );

        RequireTrue(symbolNames.Contains(
          code.Formal),
          "Failed to find exec fn arg: " + code.Formal
          );

        var execArg = instance.GetSymbol(code.Formal);
        RequireTrue(
          execArg.IsVector() || execArg.IsList(),
          $"Expecting {code.Formal} to be a list or vector"
          );
        _execFormalType = execArg.Type;

        _pathToLastSourcedFile = sourceFile.FullName;
        _lastSourcedFileLastWriteTime = sourceFile.LastWriteTimeUtc;
      }

      var statements = new List<string>();

      RequireTrue(
        _execFormalType == SymbolicExpressionType.List ||
        _execFormalType == SymbolicExpressionType.NumericVector
        );

      var assignmentFmt = _execFormalType == SymbolicExpressionType.List ?
        _execArgName + "[[\"{0}\"]] <- {1}" :
        _execArgName + "[\"{0}\"] <- {1}";

      // R clone and copy on modify
      statements.Add(_execArgName + " <- " + code.Formal);

      if (input != default)
      {
        foreach (var parameter in input.SimParameters)
        {
          var statement = string.Format(
            InvariantCulture,
            assignmentFmt,
            parameter.Name,
            parameter.GetRValue()
            );
          statements.Add(statement);
        }
      }

      var execStatement = $"{_execOutputName} <- {code.Exec}({_execArgName})";
      statements.Add(execStatement);

      var lines = string.Join(Environment.NewLine, statements);
      instance.Evaluate(lines);
    }

    public static NumDataColumn[] TabulateExecOutput(SimOutput output)
    {
      var instance = REngine.GetInstance();

      var sexpOutput = instance.GetSymbol(_execOutputName);
      var matrices = SexpToMatrices(sexpOutput);

      if (0 == matrices.Length) return Array.Empty<NumDataColumn>();

      var targetColumnNames = output.SimValues.IsEmpty 
        ? default 
        : output.SimValues.Select(v => v.Name).ToArray();

      var tabulated = new List<NumDataColumn>();

      foreach (var (columnNames, array) in matrices)
      {
        int[]? excludedColumnIndexes = default;

        if (default != targetColumnNames)
        {
          RequireNotNull(columnNames);

          excludedColumnIndexes = columnNames
               .Select((cn, i) => new { cn, i })
               .Where(a => !targetColumnNames.Contains(a.cn))
               .Select(a => a.i)
               .ToArray();
        }

        var numDataColumns = MatrixToNumDataColumns(
          array,
          columnNames,
          excludedColumnIndexes
          );

        tabulated.AddRange(numDataColumns);
      }

      return tabulated.ToArray();
    }

    public static NumDataColumn[] TabulateTmplOutput(SimConfig config)
    {
      var tabulatedData = new List<NumDataColumn>();
      var instance = REngine.GetInstance();

      foreach (var value in config.SimOutput.SimValues)
      {
        var sexp = instance.GetSymbol(value.Name);
        var matrices = SexpToMatrices(sexp, value.Name);

        foreach (var matrix in matrices)
        {
          var columnNames = matrix.ColumnNames;
          var array = matrix.Array;

          if (null == columnNames)
          {
            var nColumns = array.GetLength(1);
            RequireTrue(1 == nColumns, "Multi-column output has no column names");
            columnNames = new[] { value.Name }; // output was R vector
          }

          var toTabulate = value.SimElements.Select(e => e.Name).ToArray();

          var excludedColumnIndexes = columnNames
            .Select((cn, i) => new { cn, i })
            .Where(a => !toTabulate.Contains(a.cn))
            .Select(a => a.i)
            .ToArray();

          var numDataColumns = MatrixToNumDataColumns(
            array,
            columnNames,
            excludedColumnIndexes
            );
          tabulatedData.AddRange(numDataColumns);
        }
      }

      return tabulatedData.ToArray();
    }

    private const string _execArgName = "rvis_service_in";
    private const string _execOutputName = "rvis_service_out";
    private static string? _pathToLastSourcedFile;
    private static DateTime _lastSourcedFileLastWriteTime;
    private static SymbolicExpressionType _execFormalType;
  }
}
