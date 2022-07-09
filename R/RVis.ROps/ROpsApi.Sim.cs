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
    public static void RunExec(string pathToCode, SimInput input)
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
          !symbolNames.Contains(SimCode.R_ASSIGN_PARAMETERS) ||
          !symbolNames.Contains(SimCode.R_RUN_MODEL);
      }

      if (firstRun)
      {
        var clearOutput = $"if(exists(\"{_execOutputName}\")) rm({_execOutputName})";
        instance.Evaluate(clearOutput);

        SourceFile(sourceFile.FullName, instance);

        var symbolNames = instance.GlobalEnvironment.GetSymbolNames();

        RequireFalse(
          symbolNames.Contains(_execOutputName),
          "Script contains reserved symbol: " + _execOutputName
          );

        RequireTrue(
          symbolNames.Contains(SimCode.R_ASSIGN_PARAMETERS),
          "Failed to find parameter source function: " + SimCode.R_ASSIGN_PARAMETERS
          );
        RequireTrue(
          symbolNames.Contains(SimCode.R_RUN_MODEL),
          "Failed to find model executive function: " + SimCode.R_RUN_MODEL
          );

        var srcStatement = $"{_execSrcName} <- {SimCode.R_ASSIGN_PARAMETERS}()";
        instance.Evaluate(srcStatement);

        var execSrc = instance.GetSymbol(_execSrcName);
        RequireTrue(
          execSrc.IsVector() || execSrc.IsList(),
          $"Expecting return type from {SimCode.R_ASSIGN_PARAMETERS} to be list or vector"
          );
        _execFormalType = execSrc.Type;

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
      statements.Add(_execArgName + " <- " + _execSrcName);

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

      var execStatement = $"{_execOutputName} <- {SimCode.R_RUN_MODEL}({_execArgName})";
      statements.Add(execStatement);

      var lines = string.Join(Environment.NewLine, statements);
      instance.Evaluate(lines);
    }

    public static NumDataColumn[] TabulateExecOutput(SimOutput output)
    {
      var instance = REngine.GetInstance();
      var tabulated = new List<NumDataColumn>();

      var iv = instance.GetSymbol(output.IndependentVariable.Name).AsNumeric().ToArray();
      tabulated.Add(new NumDataColumn(output.IndependentVariable.Name, iv));

      foreach (var element in output.DependentVariables)
      {
        var dv = instance.GetSymbol(element.Name).AsNumeric().ToArray();
        tabulated.Add(new NumDataColumn(element.Name, dv));
      }

      return tabulated.ToArray();
    }

    private const string _execSrcName = "rvis_service_src";
    private const string _execArgName = "rvis_service_in";
    private const string _execOutputName = "rvis_service_out";
    private static string? _pathToLastSourcedFile;
    private static DateTime _lastSourcedFileLastWriteTime;
    private static SymbolicExpressionType _execFormalType;
  }
}
