using LanguageExt;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model.Extensions;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static System.Globalization.CultureInfo;

namespace RVis.Model
{
  public class ManagedImport : IDisposable
  {
    public ManagedImport(string pathToRFile, SimLibrary simLibrary)
    {
      _codeFileName = Path.GetFileName(pathToRFile);
      SimulationName = Path.GetFileNameWithoutExtension(_codeFileName);
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

    public string SimulationDescription { get; set; }

    public async Task InspectAsync(IRVisClient rVisClient)
    {
      var inspection = Task.Run(
        () =>
        {
          if (_pathToContainingDirectory.IsntAString()) SetUpStaging();
          var pathToCode = Path.Combine(_pathToContainingDirectory, _codeFileName);

          rVisClient.Clear();
          return rVisClient.InspectSymbols(pathToCode).ToArr();
        }
      );

      SymbolInfos = await inspection;

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
          si.Value?.DataColumns.FirstOrDefault()?.Length == 1)
        .OrderBy(si => si.Symbol)
        .ToArr();

      DataSets = SymbolInfos
        .Filter(si =>
          si.Level == 0 &&
          si.Value?.ColumnNames.All(cn => cn.IsAString()) == true &&
          si.Value?.DataColumns.FirstOrDefault()?.Length > 1)
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
      var executor = Task.Run(
        () =>
        {
          var pathToCode = Path.Combine(_pathToContainingDirectory, _codeFileName);

          var code = new SimCode(_codeFileName, function.Symbol, formal.Symbol);
          var config = new SimConfig(default, default, default, code, default, default);

          rVisClient.Clear();
          rVisClient.RunExec(pathToCode, config);
          return rVisClient.TabulateExecOutput(config);
        }
      );

      ExecutorOutput = await executor;
      ExecutorFunction = function;
      ExecutorFormal = formal;
      ExecutorParameterCandidates = ParameterCandidate.CreateForExec(ExecutorFormal, SymbolInfos);
      (ExecutorIndependentVariable, ExecutorValueCandidates) = ValueCandidate.CreateForExec(ExecutorOutput, SymbolInfos);
    }

    public ISymbolInfo ExecutorFunction { get; private set; }

    public ISymbolInfo ExecutorFormal { get; private set; }

    public Arr<ParameterCandidate> ExecutorParameterCandidates { get; private set; }

    public NumDataTable ExecutorOutput { get; private set; }

    public ValueCandidate ExecutorIndependentVariable { get; private set; }

    public Arr<ValueCandidate> ExecutorValueCandidates { get; private set; }

    public string ImportExecToLibrary()
    {
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

      return _simLibrary.Import(_pathToContainingDirectory);
    }

    public string ImportTmplToLibrary()
    {
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

      var codeLines = _codeLines.ToArray();

      foreach (var parameterCandidate in parameterCandidates)
      {
        var line = $"{parameterCandidate.SymbolInfo.Symbol} <- ${{{parameterCandidate.SymbolInfo.Symbol}}}";
        var index = parameterCandidate.SymbolInfo.LineNo - 1;
        codeLines[index] = line;
      }

      var pathToRFile = Path.Combine(_pathToContainingDirectory, _codeFileName);

      File.WriteAllLines(pathToRFile, codeLines);

      return _simLibrary.Import(_pathToContainingDirectory);
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
          Directory.Delete(_pathToContainingDirectory, true);
        }
        catch (Exception) { }

        _disposed = true;
      }
    }

    private void SetUpStaging()
    {
      var containingDirectoryName = DateTime.UtcNow.ToString("o", InvariantCulture).ToValidFileName();
      var pathToContainingDirectory = Path.Combine(Path.GetTempPath(), containingDirectoryName);
      Directory.CreateDirectory(pathToContainingDirectory);

      var pathToRFile = Path.Combine(pathToContainingDirectory, _codeFileName);

      File.WriteAllLines(pathToRFile, _codeLines);

      _pathToContainingDirectory = pathToContainingDirectory;
    }

    private readonly string _codeFileName;
    private readonly Arr<string> _codeLines;
    private readonly SimLibrary _simLibrary;
    private string _pathToContainingDirectory;
    private bool _disposed = false;
  }
}
