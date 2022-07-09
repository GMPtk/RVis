using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Double;
using static System.Environment;
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
      Import = ReactiveCommand.Create(
        HandleImportAsync,
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

    public string? PathToInFile
    {
      get => _pathToInFile;
      set => this.RaiseAndSetIfChanged(ref _pathToInFile, value);
    }
    private string? _pathToInFile;

    public bool OpenOnImport
    {
      get => _openOnImport;
      set => this.RaiseAndSetIfChanged(ref _openOnImport, value);
    }
    private bool _openOnImport = true;

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

        var pathToInFile = rootPath + ".in";
        if (File.Exists(pathToInFile))
        {
          PathToInFile = pathToInFile;
        }
        else
        {
          PathToInFile = null;
        }

        UpdateEnable();
      }
    }

    private async Task HandleImportAsync()
    {
      IsBusy = true;
      BusyWith = "Import MCSim";

      try
      {
        var (simulationName, directoryName) = await Task
          .Run(() => HandleImport())
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

        PathToExecutable = null;
        PathToInFile = null;
        UpdateEnable();
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

    private (string SimulationName, string DirectoryName) HandleImport()
    {
      RequireFile(PathToExecutable);
      RequireFile(PathToInFile);

      var executable = GetFileName(PathToExecutable);
      var simulationName = GetFileNameWithoutExtension(executable);

      var existing = _simLibrary.Simulations.Any(
        s => simulationName.Equals(s.SimConfig.Title, StringComparison.InvariantCultureIgnoreCase)
        );

      RequireFalse(existing, $"There is already a simulation with name {simulationName} in your library");

      string pathToContainingDirectory;
      do
      {
        pathToContainingDirectory = Combine(GetTempPath(), GetRandomFileName());
      }
      while (Directory.Exists(pathToContainingDirectory));

      RequireNotNull(Directory.CreateDirectory(pathToContainingDirectory));

      var lines = File
        .ReadAllLines(PathToInFile)
        .Select((l, i) => new { Index = i, Line = l })
        .ToArray();

      var reSimulation = new Regex(@"^\s*Simulation\s*{\s*$");
      var simulationLines = lines.Where(l => reSimulation.IsMatch(l.Line)).ToArray();
      RequireTrue(simulationLines.Length == 1, "Expecting one Simulation block in .in file");
      var simulationIndex = simulationLines[0].Index;

      var rePrintStep = new Regex(@"^\s*PrintStep\s*\(\s*$");
      var printStepLines = lines.Where(l => rePrintStep.IsMatch(l.Line)).ToArray();
      RequireTrue(printStepLines.Length == 1, "Expecting one PrintStep closure in .in file");
      var printStepIndex = printStepLines[0].Index;

      RequireTrue(printStepIndex > simulationIndex, "Expecting Simulation before PrintStep");

      var reEndPrintStep = new Regex(@"^\s*\)\s*;\s*$");
      var endPrintStepLines = lines.Skip(printStepIndex + 1).Where(l => reEndPrintStep.IsMatch(l.Line)).ToArray();
      RequireTrue(endPrintStepLines.Length == 1, "Expecting one PrintStep closure in .in file");
      var endPrintStepIndex = endPrintStepLines[0].Index;

      var parameters = new List<(string Name, string Value, string Description, string Unit)>();
      var reParameter = new Regex(@"^\s*(?'name'\w+)\s*=\s*(?'value'[0-9eE\/*\-\+\.]+)\s*;\s*#\s*(?'desc'[^[]*)\[(?'unit'[^\]]*)]\s*$");

      var index = simulationIndex + 1;
      Regex reTemplate;
      string templateLine;

      do
      {
        var match = reParameter.Match(lines[index].Line);
        if (match.Success)
        {
          parameters.Add((
            match.Groups["name"].Value.Trim(),
            match.Groups["value"].Value.Trim(),
            match.Groups["desc"].Value.Trim(),
            match.Groups["unit"].Value.Trim()
            ));
          reTemplate = new Regex(@"=\s*" + Regex.Escape(match.Groups["value"].Value));
          templateLine = reTemplate.Replace(lines[index].Line, $"= {{{{{match.Groups["name"].Value}}}}}", 1);
          lines[index] = new { lines[index].Index, Line = templateLine };
        }
        ++index;
      } while (index < printStepIndex);

      RequireTrue(parameters.Count > 0, "Found zero parameters");

      var outputs = new List<(string Name, string Description, string Unit)>();
      var reOutput = new Regex(@"^\s*(?'name'[\w0-9eE\-\+\.]+)\s*,?\s*#\s*(?'desc'[^[]*)\[(?'unit'[^\]]*)]\s*$");

      ++index;
      do
      {
        var match = reOutput.Match(lines[index].Line);
        if (match.Success)
        {
          outputs.Add((
            match.Groups["name"].Value.Trim(),
            match.Groups["desc"].Value.Trim(),
            match.Groups["unit"].Value.Trim()
            ));
        }
        ++index;
      } while (index < endPrintStepIndex);

      RequireTrue(outputs.Count > 0, "Found zero outputs");
      RequireTrue(outputs.Count > 3, "Expecting four or more arguments to PrintStep");

      var name = outputs[^3].Name;
      RequireTrue(TryParse(name, out double t_start), $"Expecting numerical start time; found {name}");
      const string T_START_NAME = "t_start";
      reTemplate = new Regex(Regex.Escape(name));
      templateLine = reTemplate.Replace(lines[index - 3].Line, $"{{{{{T_START_NAME}}}}}", 1);
      lines[index - 3] = new { lines[index - 3].Index, Line = templateLine };

      name = outputs[^2].Name;
      RequireTrue(TryParse(name, out double t_end), $"Expecting numerical end time; found {name}");
      const string T_END_NAME = "t_end";
      reTemplate = new Regex(Regex.Escape(name));
      templateLine = reTemplate.Replace(lines[index - 2].Line, $"{{{{{T_END_NAME}}}}}", 1);
      lines[index - 2] = new { lines[index - 2].Index, Line = templateLine };

      name = outputs[^1].Name;
      RequireTrue(TryParse(name, out double t_int), $"Expecting numerical time step; found {name}");
      const string T_INT_NAME = "t_int";
      reTemplate = new Regex(Regex.Escape(name));
      templateLine = reTemplate.Replace(lines[index - 1].Line, $"{{{{{T_INT_NAME}}}}}", 1);
      lines[index - 1] = new { lines[index - 1].Index, Line = templateLine };

      var pathToExecutable = Combine(pathToContainingDirectory, executable);
      File.Copy(PathToExecutable, pathToExecutable);

      var pathToInFile = Combine(pathToContainingDirectory, simulationName + ".in");
      File.WriteAllLines(pathToInFile, lines.Select(l => l.Line));

      var simParameters = parameters
        .Select(t => new SimParameter(t.Name, t.Value, t.Unit.RejectEmpty(), t.Description.RejectEmpty()))
        .ToArr();

      RequireFalse(simParameters.Any(p => IsNaN(p.Scalar)), "One or more parameter values is not a numeric literal");

      var tStartParameter = new SimParameter(
        T_START_NAME,
        t_start.ToString(),
        outputs[^3].Unit,
        "start time"
        );

      var tEndParameter = new SimParameter(
        T_END_NAME,
        t_end.ToString(),
        outputs[^2].Unit,
        "end time"
        );

      var tIntParameter = new SimParameter(
        T_INT_NAME,
        t_int.ToString(),
        outputs[^1].Unit,
        "time interval"
        );

      simParameters = (simParameters + tStartParameter + tEndParameter + tIntParameter)
        .OrderBy(p => p.Name)
        .ToArr();

      var outputValues = outputs
        .Take(outputs.Count - 3)
        .Select(t => new SimValue(
          t.Name,
          Array(new SimElement(t.Name, isIndependentVariable: false, t.Unit.RejectEmpty(), t.Description.RejectEmpty()))
          )
        )
        .ToArr();

      var independentVariableValue = new SimValue(
        "Time",
        Array(new SimElement(
          "Time",
          isIndependentVariable: true,
          outputs[^3].Unit,
          "sim time"
          ))
        );

      var simValues = (outputValues + independentVariableValue)
        .OrderBy(v => v.Name)
        .ToArr();

      const string DESCRIPTION_PREFIX = "# Description:";
      var descriptionLine = lines.FirstOrDefault(l => l.Line.StartsWith(DESCRIPTION_PREFIX));
      var simulationDescription = descriptionLine == default
        ? default
        : descriptionLine.Line[DESCRIPTION_PREFIX.Length..].Trim();

      var config = new SimConfig(
        simulationName,
        simulationDescription,
        DateTime.UtcNow,
        new SimCode(executable),
        new SimInput(simParameters, isDefault: true),
        new SimOutput(simValues)
        );

      config.WriteToFile(pathToContainingDirectory);

      using var executor = new MCSimExecutor(pathToContainingDirectory, config);
      var _ = executor.Execute(default);

      var directoryName = _simLibrary.ImportExeSimulation(pathToContainingDirectory, simulationName);

      return (simulationName, directoryName);
    }

    private void UpdateEnable()
    {
      CanImport = PathToExecutable.IsAString() && PathToInFile.IsAString();
    }

    private readonly SimLibrary _simLibrary;
    private readonly IAppState _appState;
    private readonly IAppService _appService;
  }
}
