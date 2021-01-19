using LanguageExt;
using RVis.Data;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using static RVis.Base.Check;
using static System.Double;
using static System.Environment;
using static System.IO.Directory;
using static System.IO.File;
using static System.IO.Path;
using static System.String;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace RVis.Model
{
  public sealed class MCSimExecutor : IDisposable
  {
    public MCSimExecutor(string pathToSimulation, SimConfig config)
    {
      RequireDirectory(pathToSimulation);
      RequireNotNullEmptyWhiteSpace(config.SimCode.File);

      var pathToExecutable = Combine(pathToSimulation, config.SimCode.File);
      RequireFile(pathToExecutable);

      var templateInFileName = GetFileNameWithoutExtension(config.SimCode.File) + ".in";
      var pathToTemplateIn = Combine(pathToSimulation, templateInFileName);
      RequireFile(pathToTemplateIn);

      var templateIn = ReadAllText(pathToTemplateIn);
      _templateIn = Template.ParseLiquid(templateIn);

      var scriptVariables = _templateIn.Page.Body.Statements
        .OfType<ScriptExpressionStatement>()
        .Select(ses => ses.Expression)
        .OfType<ScriptVariable>()
        .Select(e => e.Name)
        .OrderBy(s => s)
        .ToArr();
      var configParameters = config.SimInput.SimParameters.Map(p => p.Name);

      if (scriptVariables != configParameters)
      {
        var missingScriptVariables = configParameters.Filter(p => !scriptVariables.Contains(p));
        RequireTrue(
          missingScriptVariables.IsEmpty,
          "Missing parameters in .in file: " + Join(", ", missingScriptVariables)
          );

        var missingConfigParameters = scriptVariables.Filter(p => !configParameters.Contains(p));
        RequireTrue(
          missingConfigParameters.IsEmpty,
          "Unknown parameters in .in file: " + Join(", ", missingConfigParameters)
          );
      }

      do
      {
        _pathToInOutDirectory = Combine(GetTempPath(), GetRandomFileName());
      }
      while (Directory.Exists(_pathToInOutDirectory));

      RequireNotNull(CreateDirectory(_pathToInOutDirectory));

      _pathToInFile = Combine(
        _pathToInOutDirectory,
        GetFileNameWithoutExtension(pathToExecutable) + ".in");

      _pathToOutFile = Combine(
        _pathToInOutDirectory,
        GetFileNameWithoutExtension(pathToExecutable) + ".out");

      _processStartInfo = new ProcessStartInfo
      {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        FileName = pathToExecutable,
        Arguments = _pathToInFile + " " + _pathToOutFile
      };

      _independentVariableName = config.SimOutput.IndependentVariable.Name;
      _dependentVariableNames = config.SimOutput.DependentVariables.Map(v => v.Name);
      _tableName = config.Title;
      _defaultSource = config.SimInput.SimParameters;
    }

    public void Dispose() => Dispose(true);

    public NumDataTable? Execute(Arr<SimParameter> parameters)
    {
      if (!Monitor.TryEnter(_syncLock))
      {
        throw new InvalidOperationException("Executor is busy");
      }

      try
      {
        return _Execute(parameters);
      }
      finally
      {
        Monitor.Exit(_syncLock);
      }
    }

    private NumDataTable? _Execute(Arr<SimParameter> parameters)
    {
      File.Delete(_pathToOutFile);

      var source = _defaultSource.Map(
        p => parameters.Find(pp => pp.Name == p.Name).IfNone(p)
        );

      var scriptObject = new ScriptObject();
      source.Iter(p => scriptObject.Add(p.Name, p.Value));

      var context = new TemplateContext();
      context.PushGlobal(scriptObject);

      var @in = _templateIn.Render(context);

      WriteAllText(_pathToInFile, @in);

      using var run = Process.Start(_processStartInfo);

      RequireNotNull(run, "Failed to start MCSim process");

      run.WaitForExit();

      if (run.ExitCode != 0)
      {
        var stdOut = run.StandardOutput.ReadToEnd();
        var stdErr = run.StandardError.ReadToEnd();
        return HandleFailure(run.ExitCode, stdOut, stdErr);
      }

      if (!File.Exists(_pathToOutFile))
      {
        var stdOut = run.StandardOutput.ReadToEnd();
        return HandleIntegrationFailure(stdOut);
      }

      return ProcessOutFile();
    }

    private NumDataTable? ProcessOutFile()
    {
      var @out = ReadAllLines(_pathToOutFile);

      RequireTrue(@out[0].StartsWith("Results"), "Unexpected out file format");

      var line = 1;

      while (line < @out.Length && !@out[line].StartsWith(_independentVariableName)) ++line;

      if (line == @out.Length) return default;

      var outputNames = @out[line]
        .Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries)
        .ToArr();

      RequireTrue(
        outputNames.Skip(1).OrderBy(n => n).SequenceEqual(_dependentVariableNames),
        $"Expected output columns: {Join(", ", _dependentVariableNames)}. Received output columns: {Join(", ", outputNames.Skip(1))}"
        );

      var nRows = @out.Length - line - 1;

      var outputColumns = new SortedDictionary<string, List<double>>();
      outputNames.Iter(@on => outputColumns.Add(on, new List<double>(nRows)));

      ++line;

      while (line < @out.Length)
      {
        var row = @out[line].Trim();

        if (row.Length == 0) break;

        var values = row
          .Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries)
          .Map(v => TryParse(v, out double d) ? d : NaN)
          .ToArr();

        outputNames.Iter((i, @on) => outputColumns[@on].Add(values[i]));

        ++line;
      }

      RequireTrue(
        outputColumns.Values
          .Skip(1)
          .All(v => v.Count == outputColumns.Values.First().Count)
        );

      var table = new NumDataTable(
        _tableName,
        outputNames.Map(@on => new NumDataColumn(on, outputColumns[@on]))
        );

      return table;
    }

    private static NumDataTable HandleFailure(int exitCode, string stdOut, string stdErr)
    {
      static Arr<string> ExtractErrors(string s)
      {
        const string PREFIX = "Error:";

        var lines = s.Split(new[] { NewLine }, StringSplitOptions.RemoveEmptyEntries);

        return lines
          .Where(l => l.StartsWith(PREFIX))
          .Select(l => l[PREFIX.Length..].Trim())
          .ToArr();
      }

      var errorsFromOut = ExtractErrors(stdOut);
      var errorsFromErr = ExtractErrors(stdErr);

      throw new MCSimExecutionException(
        $"MCSim Fault ({Convert.ToString(exitCode, 2)})",
        exitCode,
        errorsFromOut + errorsFromErr
        );
    }

    private static NumDataTable HandleIntegrationFailure(string stdOut)
    {
      const string START_PREFIX = "Doing analysis";

      var lines = stdOut.Split(new[] { NewLine }, StringSplitOptions.RemoveEmptyEntries);
      var startIndex = 0;

      while (!lines[startIndex].StartsWith(START_PREFIX) && ++startIndex < lines.Length) ;

      if (++startIndex >= lines.Length)
      {
        throw new MCSimExecutionException("Unknown integration fault", 0, default);
      }

      const string END_PREFIX = "Done";

      var endIndex = startIndex;

      while (!lines[endIndex].StartsWith(END_PREFIX) && ++endIndex < lines.Length) ;

      if (endIndex >= lines.Length)
      {
        throw new MCSimExecutionException("Unknown integration fault", 0, default);
      }

      var diagnostics = lines.Skip(startIndex).Take(endIndex - startIndex).ToArr();

      throw new MCSimExecutionException("MCSim failed to produce data", 0, diagnostics);
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {

        }

        try
        {
          Delete(_pathToInOutDirectory, recursive: true);
        }
        catch (Exception) { }

        _disposed = true;
      }
    }

    private readonly Template _templateIn;
    private readonly string _pathToInOutDirectory;
    private readonly string _pathToInFile;
    private readonly string _pathToOutFile;
    private readonly ProcessStartInfo _processStartInfo;
    private readonly string _independentVariableName;
    private readonly Arr<string> _dependentVariableNames;
    private readonly string _tableName;
    private readonly Arr<SimParameter> _defaultSource;
    private readonly object _syncLock = new object();
    private bool _disposed = false;
  }
}
