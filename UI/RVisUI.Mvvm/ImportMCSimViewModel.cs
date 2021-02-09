using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Double;
using static System.Environment;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using static System.String;

namespace RVisUI.Mvvm
{
  public sealed class ImportMCSimViewModel : ReactiveObject, IImportMCSimViewModel
  {
    public ImportMCSimViewModel(SimLibrary simLibrary, IAppState appState, IAppService appService)
    {
      _simLibrary = simLibrary;
      _appState = appState;
      _appService = appService;

      BrowseForExecutable = ReactiveCommand.Create(HandleBrowseForExecutable);
      BrowseForConfigurationFile = ReactiveCommand.Create(HandleBrowseForConfigurationFile);
      BrowseForTemplateInFile = ReactiveCommand.Create(HandleBrowseForTemplateInFile);
      Import = ReactiveCommand.Create(
        HandleImport,
        this.ObservableForProperty(vm => vm.CanImport, _ => CanImport)
        );

      UpdateEnable();
    }

    public ICommand BrowseForExecutable { get; }

    public string? PathToExecutable
    {
      get => _pathToExecutable;
      set => this.RaiseAndSetIfChanged(ref _pathToExecutable, value);
    }
    private string? _pathToExecutable;

    public ICommand BrowseForConfigurationFile { get; }

    public string? PathToConfigurationFile
    {
      get => _pathToConfigurationFile;
      set => this.RaiseAndSetIfChanged(ref _pathToConfigurationFile, value);
    }
    private string? _pathToConfigurationFile;

    public ICommand BrowseForTemplateInFile { get; }

    public string? PathToTemplateInFile
    {
      get => _pathToTemplateInFile;
      set => this.RaiseAndSetIfChanged(ref _pathToTemplateInFile, value);
    }
    private string? _pathToTemplateInFile;

    public bool OpenOnImport
    {
      get => _openOnImport;
      set => this.RaiseAndSetIfChanged(ref _openOnImport, value);
    }
    private bool _openOnImport;

    public ICommand Import { get; }

    public bool CanImport
    {
      get => _canImport;
      set => this.RaiseAndSetIfChanged(ref _canImport, value);
    }
    private bool _canImport;

    public bool IsBusy
    {
      get => _isBusy;
      set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
    private bool _isBusy;

    public string? BusyWith
    {
      get => _busyWith;
      set => this.RaiseAndSetIfChanged(ref _busyWith, value);
    }
    private string? _busyWith;

    private void HandleBrowseForExecutable()
    {
      var initialDirectory = PathToExecutable.IsAString()
        ? GetDirectoryName(PathToExecutable)
        : default;

      var didBrowse = _appService.OpenFile(
        "Select MCSim Executable",
        initialDirectory,
        "Executable Files|*.exe",
        out string? pathToFile
        );

      if (didBrowse)
      {
        PathToExecutable = pathToFile;

        var rootDir = GetDirectoryName(pathToFile);
        RequireDirectory(rootDir);
        var rootName = GetFileNameWithoutExtension(pathToFile);
        RequireNotNullEmptyWhiteSpace(rootName);
        var rootPath = Combine(rootDir, rootName);

        var pathToConfigurationFile = rootPath + ".config.R";
        if (File.Exists(pathToConfigurationFile))
        {
          PathToConfigurationFile = pathToConfigurationFile;
        }

        var pathToTemplateInFile = rootPath + ".template.in";
        if (File.Exists(pathToTemplateInFile))
        {
          PathToTemplateInFile = pathToTemplateInFile;
        }

        UpdateEnable();
      }
    }

    private void HandleBrowseForConfigurationFile()
    {
      var initialDirectory = PathToConfigurationFile.IsAString()
        ? GetDirectoryName(PathToConfigurationFile)
        : default;

      var didBrowse = _appService.OpenFile(
        "Select Configuration File",
        initialDirectory,
        "R Files|*.R",
        out string? pathToFile
        );

      if (didBrowse)
      {
        PathToConfigurationFile = pathToFile;
        UpdateEnable();
      }
    }

    private void HandleBrowseForTemplateInFile()
    {
      var initialDirectory = PathToTemplateInFile.IsAString()
        ? GetDirectoryName(PathToTemplateInFile)
        : default;

      var didBrowse = _appService.OpenFile(
        "Select Template .in File",
        initialDirectory,
        "in Files|*.in",
        out string? pathToFile
        );

      if (didBrowse)
      {
        PathToTemplateInFile = pathToFile;
        UpdateEnable();
      }
    }

    private void HandleImport()
    {
      async Task SomeServer(ServerLicense serverLicense)
      {
        IsBusy = true;
        BusyWith = "Import MCSim";

        try
        {
          using (serverLicense)
          {
            var (simulationName, directoryName) = await HandleImportAsync(await serverLicense.GetRClientAsync())
              .ConfigureAwait(continueOnCapturedContext: true);

            _appState.Status = $"Imported {simulationName}";

            _simLibrary.Refresh();

            if (OpenOnImport)
            {
              var lookup = $"{DirectorySeparatorChar}{directoryName}";

              var simulation = _simLibrary.Simulations.Find(
                s => s.PathToSimulation.EndsWith(directoryName)
                );

              _appState.Target = simulation;
            }
          }
        }
        catch (MCSimExecutionException ex)
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(ImportMCSimViewModel),
            nameof(HandleImportAsync),
            ex.Message + NewLine + NewLine + Join(NewLine, ex.Diagnostics),
            this
            );
        }
        catch (Exception ex)
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(ImportMCSimViewModel),
            nameof(HandleImportAsync),
            ex.Message,
            this
            );
        }

        IsBusy = false;
      }

      async Task NoServer()
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(ImportMCSimViewModel),
          nameof(HandleImportAsync),
          "No R server available",
          this
          );

        await Task.CompletedTask;
      }

      var _ = _appService.RVisServerPool.RequestServer().Match(SomeServer, NoServer);
    }

    private async Task<(string SimulationName, string DirectoryName)> HandleImportAsync(IRVisClient client)
    {
      RequireFile(PathToConfigurationFile);
      RequireFile(PathToExecutable);
      RequireFile(PathToTemplateInFile);

      string pathToContainingDirectory;
      do
      {
        pathToContainingDirectory = Combine(GetTempPath(), GetRandomFileName());
      }
      while (Directory.Exists(pathToContainingDirectory));

      RequireNotNull(Directory.CreateDirectory(pathToContainingDirectory));

      await client.ClearAsync().ConfigureAwait(continueOnCapturedContext: false);
      var inspection = await client.InspectSymbolsAsync(PathToConfigurationFile).ConfigureAwait(continueOnCapturedContext: false);
      var symbolInfos = inspection.ToArr();

      var reAssignment = new Regex("(\\w+)\\W*=\\W*([^\"]+)");

      var assignments = symbolInfos
        .Select(si =>
        {
          if (si.Code.IsntAString()) return None;
          var match = reAssignment.Match(si.Code);
          if (!match.Success) return None;
          var symbol = match.Groups[1].Value;
          var assignment = match.Groups[2].Value.Trim();
          return Some((Symbol: symbol, Assignment: assignment, SymbolInfo: si));
        })
        .Somes()
        .ToArray();

      string? simulationName;
      simulationName = (
        from assignment in assignments
        where assignment.Symbol == nameof(simulationName)
        select assignment.Assignment
        ).SingleOrDefault();
      RequireNotNullEmptyWhiteSpace(simulationName, "Simulation name not specified");

      string? description;
      description = (
        from assignment in assignments
        where assignment.Symbol == nameof(description)
        select assignment.Assignment
        ).SingleOrDefault();

      string? importName;
      importName = (
        from assignment in assignments
        where assignment.Symbol == nameof(importName)
        select assignment.Assignment
        ).SingleOrDefault();
      RequireNotNullEmptyWhiteSpace(importName, "Import name not specified");

      ISymbolInfo parameters;
      parameters = symbolInfos.SingleOrDefault(si => si.Symbol == nameof(parameters));
      RequireNotNull(parameters, $"{nameof(parameters)} section not found in configuration file");
      RequireNotNull(parameters.Names, "Unable to read parameter names from configuration file");

      var simParameters = parameters.Names
        .Select(pn =>
        {
          var parameterAssignment = assignments.Single(a => a.Symbol == pn);

          RequireTrue(
            TryParse(parameterAssignment.Assignment, out double _),
            $"parameter {pn} does not have scalar assignment"
            );

          return new SimParameter(
            pn,
            parameterAssignment.Assignment,
            parameterAssignment.SymbolInfo.Unit,
            parameterAssignment.SymbolInfo.Comment
            );
        })
        .OrderBy(p => p.Name)
        .ToArr();

      ISymbolInfo independentVariable;
      independentVariable = symbolInfos.SingleOrDefault(si => si.Symbol == nameof(independentVariable));
      RequireNotNull(independentVariable, $"{nameof(independentVariable)} section not found in configuration file");
      RequireNotNull(independentVariable.Names, "Unable to read independent variable name from configuration file");

      var independentVariableName = independentVariable.Names.Single();
      var ivAssignment = assignments.Single(si => si.Symbol == independentVariableName);
      var independentVariableValue = new SimValue(
        independentVariableName,
        Array(new SimElement(
          independentVariableName,
          isIndependentVariable: true,
          ivAssignment.SymbolInfo.Unit,
          ivAssignment.SymbolInfo.Comment
          ))
        );

      ISymbolInfo outputs;
      outputs = symbolInfos.SingleOrDefault(si => si.Symbol == nameof(outputs));
      RequireNotNull(outputs, $"{nameof(outputs)} section not found in configuration file");
      RequireNotNull(outputs.Names, "Unable to read output names from configuration file");

      var outputValues = outputs.Names
        .Select(on =>
        {
          var outputAssignment = assignments.Single(a => a.Symbol == on);

          var simElement = new SimElement(
            on,
            isIndependentVariable: false,
            outputAssignment.SymbolInfo.Unit,
            outputAssignment.SymbolInfo.Comment
            );

          return new SimValue(on, Array(simElement));
        })
        .ToArr();

      var simValues = (independentVariableValue + outputValues).OrderBy(p => p.Name).ToArr();

      var executable = GetFileName(PathToExecutable);

      var config = new SimConfig(
        simulationName,
        description,
        DateTime.UtcNow,
        new SimCode(executable, default, default),
        new SimInput(simParameters, isDefault: true),
        new SimOutput(simValues)
        );

      config.WriteToFile(pathToContainingDirectory);

      var pathToExecutable = Combine(pathToContainingDirectory, executable);
      File.Copy(PathToExecutable, pathToExecutable);

      var templateInFileName = GetFileNameWithoutExtension(executable) + ".in";
      var pathToTemplateInFile = Combine(pathToContainingDirectory, templateInFileName);
      File.Copy(PathToTemplateInFile, pathToTemplateInFile);

      using var executor = new MCSimExecutor(pathToContainingDirectory, config);
      var _ = executor.Execute(default);

      var directoryName = _simLibrary.ImportExeSimulation(pathToContainingDirectory, importName);

      return (simulationName, directoryName);
    }

    private void UpdateEnable()
    {
      CanImport = PathToExecutable.IsAString() && PathToConfigurationFile.IsAString();
    }

    private readonly SimLibrary _simLibrary;
    private readonly IAppState _appState;
    private readonly IAppService _appService;
  }
}
