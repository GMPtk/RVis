using LanguageExt;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Data.NumDataTable;
using static RVis.Model.Constant;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using static RVis.Base.Check;

namespace RVis.Model
{
  public class ManagedImport : IDisposable
  {
    public ManagedImport(string pathToRFile, SimLibrary simLibrary)
    {
      _codeFileName = GetFileName(pathToRFile);
      SimulationName = GetFileNameWithoutExtension(_codeFileName);
      _codeLines = File.ReadAllLines(pathToRFile);
      _simLibrary = simLibrary;
    }

    ~ManagedImport() => Dispose(false);

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public string SimulationName { get; set; }

    public string? SimulationDescription { get; set; }

    public async Task InspectAsync(IRVisClient rVisClient)
    {
      if (_pathToContainingDirectory.IsntAString()) SetUpStaging();
      RequireDirectory(_pathToContainingDirectory);
      var pathToCode = Combine(_pathToContainingDirectory, _codeFileName);

      await rVisClient.ClearAsync();
      var inspection = await rVisClient.InspectSymbolsAsync(pathToCode);

      SymbolInfos = inspection.ToArr();

      UnaryFunctions = SymbolInfos
        .Filter(si =>
          si.Level == 0 &&
          si.Symbol.IsAString() &&
          si.SymbolType == SymbolType.Function &&
          si.Length == 1)
        .OrderBy(si => si.Symbol)
        .ToArr();

      Scalars = SymbolInfos
        .Filter(si =>
          si.Level == 0 &&
          si.Symbol.IsAString() &&
          si.SymbolType == SymbolType.Vector &&
          si.Length == 1 &&
          si.Scalar.HasValue)
        .OrderBy(si => si.Symbol)
        .ToArr();

      ScalarSets = SymbolInfos
        .Filter(si =>
          si.Level == 0 &&
          si.Symbol.IsAString() &&
          (si.SymbolType == SymbolType.Vector || si.SymbolType == SymbolType.List) &&
          si.Names?.Length > 1 &&
          si.Value?.NRows == 1)
        .OrderBy(si => si.Symbol)
        .ToArr();

      DataSets = SymbolInfos
        .Filter(si =>
          si.Level == 0 &&
          si.Value?.ColumnNames.All(cn => cn.IsAString()) == true &&
          si.Value?.NRows > 1)
        .OrderBy(si => si.Symbol)
        .ToArr();

      ParameterCandidates = Scalars.Map(s => new ParameterCandidate(s));

      ValueCandidates = DataSets.Map(ds => new ValueCandidate(ds, SymbolInfos));
    }

    public Arr<ISymbolInfo> SymbolInfos { get; private set; }

    public Arr<ISymbolInfo> UnaryFunctions { get; private set; }

    public Arr<ISymbolInfo> Scalars { get; private set; }

    public Arr<ISymbolInfo> ScalarSets { get; private set; }

    public Arr<ISymbolInfo> DataSets { get; private set; }

    public Arr<ParameterCandidate> ParameterCandidates { get; private set; }

    public Arr<ValueCandidate> ValueCandidates { get; private set; }

    public async Task SetExecutorAsync(ISymbolInfo function, ISymbolInfo formal, IRVisClient rVisClient)
    {
      RequireDirectory(_pathToContainingDirectory);
      var pathToCode = Combine(_pathToContainingDirectory, _codeFileName);

      var code = new SimCode(_codeFileName, function.Symbol, formal.Symbol);
      var config = new SimConfig("Managed import", "dummy", default, code, default, default);

      await rVisClient.ClearAsync();
      await rVisClient.RunExecAsync(pathToCode, config);
      var output = await rVisClient.TabulateExecOutputAsync(config);

      ExecutorOutput = output;
      ExecutorFunction = function;
      ExecutorFormal = formal;
      ExecutorParameterCandidates = ParameterCandidate.CreateForExec(ExecutorFormal, SymbolInfos);
      (ExecutorIndependentVariable, ExecutorValueCandidates) = ValueCandidate.CreateForExec(ExecutorOutput, SymbolInfos);
    }

    public ISymbolInfo? ExecutorFunction { get; private set; }

    public ISymbolInfo? ExecutorFormal { get; private set; }

    public Arr<ParameterCandidate> ExecutorParameterCandidates { get; private set; }

    public NumDataTable? ExecutorOutput { get; private set; }

    public ValueCandidate? ExecutorIndependentVariable { get; private set; }

    public Arr<ValueCandidate> ExecutorValueCandidates { get; private set; }

    public async Task<string> ImportExecToLibraryAsync(IRVisClient rVisClient)
    {
      RequireNotNull(ExecutorFunction);
      RequireNotNull(ExecutorFormal);
      RequireNotNull(ExecutorIndependentVariable);
      RequireDirectory(_pathToContainingDirectory);

      var code = new SimCode(_codeFileName, ExecutorFunction.Symbol, ExecutorFormal.Symbol);

      var parameters = ExecutorParameterCandidates
        .Filter(pc => pc.IsUsed)
        .Map(pc => new SimParameter(
          pc.Name,
          pc.Value.ToString(InvariantCulture),
          pc.Unit,
          pc.Description
          ));

      var input = new SimInput(parameters, isDefault: true);

      var ivValue = new SimValue(
        ExecutorIndependentVariable.Name,
        Array(new SimElement(
          ExecutorIndependentVariable.ElementCandidates[0].Name,
          true,
          ExecutorIndependentVariable.ElementCandidates[0].Unit,
          ExecutorIndependentVariable.ElementCandidates[0].Description
          ))
        );

      var values = ExecutorValueCandidates
        .Filter(vc => vc.ElementCandidates[0].IsUsed)
        .Map(vc => new SimValue(
          vc.Name,
          Array(
            new SimElement(
              vc.ElementCandidates[0].Name,
              false,
              vc.ElementCandidates[0].Unit,
              vc.ElementCandidates[0].Description
            )
          ))
        );


      var output = new SimOutput(ivValue + values);

      var config = new SimConfig(SimulationName, SimulationDescription, DateTime.UtcNow, code, input, output);

      config.WriteToFile(_pathToContainingDirectory);

      var pathToCode = Combine(_pathToContainingDirectory, _codeFileName);

      await rVisClient.ClearAsync();
      await rVisClient.RunExecAsync(pathToCode, config);
      var trace = await rVisClient.TabulateExecOutputAsync(config);

      CheckTrace(trace);

      return _simLibrary.ImportRSimulation(_pathToContainingDirectory);
    }

    public async Task<string> ImportTmplToLibraryAsync(IRVisClient rVisClient)
    {
      RequireDirectory(_pathToContainingDirectory);

      var code = new SimCode(_codeFileName, default, default);

      var parameterCandidates = ParameterCandidates.Filter(pc => pc.IsUsed);

      var parameters = parameterCandidates
        .Map(pc => new SimParameter(
          pc.Name,
          pc.Value.ToString(InvariantCulture),
          pc.Unit,
          pc.Description
        ));

      var input = new SimInput(parameters, isDefault: true);

      var values = ValueCandidates
        .Filter(vc => vc.ElementCandidates.Exists(ec => ec.IsUsed))
        .Map(vc => new SimValue(
          vc.Name,
          vc.ElementCandidates
            .Filter(ec => ec.IsUsed)
            .Map(ec => new SimElement(ec.Name, ec.IsIndependentVariable, ec.Unit, ec.Description))
          )
        );

      var output = new SimOutput(values);

      var config = new SimConfig(
        SimulationName,
        SimulationDescription,
        DateTime.UtcNow,
        code,
        input,
        output
        );

      config.WriteToFile(_pathToContainingDirectory);

      var pathToCode = Combine(_pathToContainingDirectory, _codeFileName);

      await rVisClient.ClearAsync();
      await rVisClient.SourceFileAsync(pathToCode);
      var trace = await rVisClient.TabulateTmplOutputAsync(config);

      CheckTrace(trace);

      var codeLines = _codeLines.ToArray();

      foreach (var parameterCandidate in parameterCandidates)
      {
        RequireNotNull(parameterCandidate.SymbolInfo);

        var line = $"{parameterCandidate.SymbolInfo.Symbol} <- ${{{parameterCandidate.SymbolInfo.Symbol}}}";
        var index = parameterCandidate.SymbolInfo.LineNo - 1;
        codeLines[index] = line;
      }

      var pathToRFile = Combine(_pathToContainingDirectory, _codeFileName);

      File.WriteAllLines(pathToRFile, codeLines);

      return _simLibrary.ImportRSimulation(_pathToContainingDirectory);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
        }

        try
        {
          if (null != _pathToContainingDirectory)
          {
            Directory.Delete(_pathToContainingDirectory, true);
          }
        }
        catch (Exception) { }

        _disposed = true;
      }
    }

    private void SetUpStaging()
    {
      string pathToContainingDirectory;
      do
      {
        pathToContainingDirectory = Combine(GetTempPath(), GetRandomFileName());
      }
      while (Directory.Exists(pathToContainingDirectory));

      RequireNotNull(Directory.CreateDirectory(pathToContainingDirectory));

      var pathToRFile = Combine(pathToContainingDirectory, _codeFileName);

      File.WriteAllLines(pathToRFile, _codeLines);

      _pathToContainingDirectory = pathToContainingDirectory;
    }

    private static void CheckTrace(NumDataTable trace)
    {
      RequireTrue(
        trace.NumDataColumns.AllUnique(ndc => ndc.Name),
        "Output contains duplicate names"
        );
    }

    private readonly string _codeFileName;
    private readonly Arr<string> _codeLines;
    private readonly SimLibrary _simLibrary;
    private string? _pathToContainingDirectory;
    private bool _disposed = false;
  }
}
