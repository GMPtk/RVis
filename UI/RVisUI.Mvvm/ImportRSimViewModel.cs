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
using static System.IO.Path;

namespace RVisUI.Mvvm
{
  public sealed class ImportRSimViewModel : ReactiveObject, IImportRSimViewModel
  {
    public ImportRSimViewModel(SimLibrary simLibrary, IAppState appState, IAppService appService)
    {
      _simLibrary = simLibrary;
      _appState = appState;
      _appService = appService;

      BrowseForScript = ReactiveCommand.Create(HandleBrowseForScript);
      Import = ReactiveCommand.Create(
        HandleImport,
        this.ObservableForProperty(vm => vm.CanImport, _ => CanImport)
        );

      UpdateEnable();
    }

    public ICommand BrowseForScript { get; }

    public string? PathToScript
    {
      get => _pathToScript;
      set => this.RaiseAndSetIfChanged(ref _pathToScript, value);
    }
    private string? _pathToScript;

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

    private void HandleBrowseForScript()
    {
      var initialDirectory = PathToScript.IsAString()
        ? GetDirectoryName(PathToScript)
        : default;

      var didBrowse = _appService.OpenFile(
        "Select R Script",
        initialDirectory,
        "Script Files|*.R",
        out string? pathToFile
        );

      if (didBrowse)
      {
        PathToScript = pathToFile;
        UpdateEnable();
      }
    }

    private void HandleImport()
    {
      async Task SomeServer(ServerLicense serverLicense)
      {
        IsBusy = true;
        BusyWith = "Import R";

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

          PathToScript = null;
          UpdateEnable();
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
      RequireFile(PathToScript);

      var script = GetFileName(PathToScript);
      var simulationName = GetFileNameWithoutExtension(script);

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
        .ReadAllLines(PathToScript)
        .Select((l, i) => new { Index = i, Line = l })
        .ToArray();

      var reBeginParameters = new Regex(@"^\s*#\s*BEGIN\s+PARAMETERS\s*$");
      var beginParametersLines = lines.Where(l => reBeginParameters.IsMatch(l.Line)).ToArray();
      RequireTrue(beginParametersLines.Length == 1, "Expecting one PARAMETERS block in script");
      var beginParametersIndex = beginParametersLines[0].Index;

      var reEndParameters = new Regex(@"^\s*#\s*END\s+PARAMETERS\s*$");
      var endParametersLines = lines.Where(l => reEndParameters.IsMatch(l.Line)).ToArray();
      RequireTrue(endParametersLines.Length == 1, "Expecting one PARAMETERS block in script");
      var endParametersIndex = endParametersLines[0].Index;

      RequireTrue(endParametersIndex > beginParametersIndex, "Invalid PARAMETERS block");

      var reBeginOutputs = new Regex(@"^\s*#\s*BEGIN\s+OUTPUTS\s*$");
      var beginOutputsLines = lines.Where(l => reBeginOutputs.IsMatch(l.Line)).ToArray();
      RequireTrue(beginOutputsLines.Length == 1, "Expecting one OUTPUTS block in script");
      var beginOutputsIndex = beginOutputsLines[0].Index;

      var reEndOutputs = new Regex(@"^\s*#\s*END\s+OUTPUTS\s*$");
      var endOutputsLines = lines.Where(l => reEndOutputs.IsMatch(l.Line)).ToArray();
      RequireTrue(endOutputsLines.Length == 1, "Expecting one OUTPUTS block in script");
      var endOutputsIndex = endOutputsLines[0].Index;

      RequireTrue(endOutputsIndex > beginOutputsIndex, "Invalid OUTPUTS block");

      var parameters = new List<(string Name, string Value, string Description, string Unit)>();
      var reParameter = new Regex(@"^\s*(?'name'\w+)\s*<-\s*(?'value'[0-9eE\/*\-\+\.]+)\s*#\s*(?'desc'[^[]*)\[(?'unit'[^\]]*)]\s*$");

      var index = beginParametersIndex + 1;

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
        }
        ++index;
      } while (index < endParametersIndex);

      RequireTrue(parameters.Count > 0, "Found zero parameters");

      var outputs = new List<(string Name, string Description, string Unit)>();
      var reOutput = new Regex(@"^\s*(?'name'[\w]+)\s*<<-[^#]*#\s*(?'desc'[^[]*)\[(?'unit'[^\]]*)]\s*$");

      index = beginOutputsIndex + 1;
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
      } while (index < endOutputsIndex);

      RequireTrue(outputs.Count > 0, "Found zero outputs");
      RequireTrue(outputs.Count > 1, "Expecting two or more outputs");

      var pathToScript = Combine(pathToContainingDirectory, script);
      File.Copy(PathToScript, pathToScript);

      var simParameters = parameters
        .Select(t => new SimParameter(t.Name, t.Value, t.Unit.RejectEmpty(), t.Description.RejectEmpty()))
        .OrderBy(p => p.Name)
        .ToArr();

      RequireFalse(simParameters.Any(p => IsNaN(p.Scalar)), "One or more parameter values is not a numeric literal");

      var simValues = outputs
        .Select((t, i) => new SimValue(
          t.Name,
          Array(new SimElement(t.Name, isIndependentVariable: i == 0, t.Unit.RejectEmpty(), t.Description.RejectEmpty()))
          )
        )
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
        new SimCode(script),
        new SimInput(simParameters, isDefault: true),
        new SimOutput(simValues)
        );

      config.WriteToFile(pathToContainingDirectory);

      await client.RunExecAsync(pathToScript, config);
      var _ = await client.TabulateExecOutputAsync(config);

      var directoryName = _simLibrary.ImportRSimulation(pathToContainingDirectory, simulationName);

      return (simulationName, directoryName);
    }

    private void UpdateEnable()
    {
      CanImport = PathToScript.IsAString();
    }

    private readonly SimLibrary _simLibrary;
    private readonly IAppState _appState;
    private readonly IAppService _appService;
  }
}
