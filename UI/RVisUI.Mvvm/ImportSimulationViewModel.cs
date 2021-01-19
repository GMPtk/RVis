using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using RVisUI.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using static RVis.Base.Check;

namespace RVisUI.Mvvm
{
  public class ImportSimulationViewModel : ReactiveObject, IImportSimulationViewModel
  {
    public ImportSimulationViewModel(
      SimLibrary simLibrary,
      IAppState appState,
      IAppService appService
      )
    {
      _simLibrary = simLibrary;
      _appState = appState;
      _appService = appService;

      BrowseForRFile = ReactiveCommand.Create(HandleBrowseForRFile);

      InspectRFile = ReactiveCommand.Create(
        HandleInspectRFile,
        this.ObservableForProperty(vm => vm.PathToRFile, p => p.IsAString())
        );

      SelectExecutive = ReactiveCommand.Create(
        HandleSelectExec,
        this.WhenAny(
          vm => vm.UnaryFuncs,
          vm => vm.ScalarSets,
          (ufs, sss) => ufs.Value.Count > 0 && sss.Value.Count > 0
          )
        );

      ImportUsingExec = ReactiveCommand.Create(
        HandleImportUsingExec,
        this.ObservableForProperty(vm => vm.ExecutiveFunction, si => si != default)
        );

      ImportUsingTmpl = ReactiveCommand.Create(
        HandleImportUsingTmpl,
        this.ObservableForProperty(vm => vm.ManagedImport, mi => mi != default)
        );

      BusyCancel = ReactiveCommand.Create(HandleBusyCancel);
    }

    public ICommand BrowseForRFile { get; }

    public string? PathToRFile
    {
      get => _pathToRFile;
      set => this.RaiseAndSetIfChanged(ref _pathToRFile, value);
    }
    private string? _pathToRFile;

    public ICommand InspectRFile { get; }

    public ManagedImport? ManagedImport
    {
      get => _managedImport;
      set => this.RaiseAndSetIfChanged(ref _managedImport, value);
    }
    private ManagedImport? _managedImport;

    public Arr<ISymbolInfo> UnaryFuncs
    {
      get => _unaryFuncs;
      set => this.RaiseAndSetIfChanged(ref _unaryFuncs, value);
    }
    private Arr<ISymbolInfo> _unaryFuncs;

    public Arr<ISymbolInfo> Scalars
    {
      get => _scalars;
      set => this.RaiseAndSetIfChanged(ref _scalars, value);
    }
    private Arr<ISymbolInfo> _scalars;

    public Arr<ISymbolInfo> ScalarSets
    {
      get => _scalarsets;
      set => this.RaiseAndSetIfChanged(ref _scalarsets, value);
    }
    private Arr<ISymbolInfo> _scalarsets;

    public Arr<ISymbolInfo> DataSets
    {
      get => _dataSets;
      set => this.RaiseAndSetIfChanged(ref _dataSets, value);
    }
    private Arr<ISymbolInfo> _dataSets;

    public ISymbolInfo? ExecutiveFunction
    {
      get => _executiveFunction;
      set => this.RaiseAndSetIfChanged(ref _executiveFunction, value);
    }
    private ISymbolInfo? _executiveFunction;

    public ISymbolInfo? ExecutiveFormal
    {
      get => _executiveFormal;
      set => this.RaiseAndSetIfChanged(ref _executiveFormal, value);
    }
    private ISymbolInfo? _executiveFormal;

    public NumDataTable? ExecutiveOutput
    {
      get => _executiveOutput;
      set => this.RaiseAndSetIfChanged(ref _executiveOutput, value);
    }
    private NumDataTable? _executiveOutput;

    public ICommand SelectExecutive { get; }

    public ICommand ImportUsingExec { get; }

    public ICommand ImportUsingTmpl { get; }

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

    public ObservableCollection<string>? BusyMessages => default;

    public bool EnableBusyCancel
    {
      get => _enableBusyCancel;
      set => this.RaiseAndSetIfChanged(ref _enableBusyCancel, value);
    }
    private bool _enableBusyCancel;

    public ICommand BusyCancel { get; }

    private void HandleBrowseForRFile()
    {
      var lastDirectoryBrowsed = PathToRFile.IsAString()
        ? Path.GetDirectoryName(PathToRFile)
        : default;

      var didOpen = _appService.OpenFile(
        "Import Simulation",
        lastDirectoryBrowsed,
        "R Files|*.R",
        out string? path
        );

      if (!didOpen) return;

      PathToRFile = path;

      ResetInspection();
    }

    private void HandleInspectRFile()
    {
      void Some(ServerLicense serverLicense)
      {
        ResetInspection();
        var _ = RunInspectionAsync(serverLicense);
      }
      void None()
      {
        _appService.Notify(
          NotificationType.Information,
          nameof(ImportSimulationViewModel),
          nameof(HandleInspectRFile),
          "No R server available",
          this
          );
      }
      _appService.RVisServerPool.RequestServer().Match(Some, None);
    }

    private async Task RunInspectionAsync(ServerLicense serverLicense)
    {
      RequireFile(PathToRFile);

      _busyCancelHandler = async () => await (await serverLicense.GetRClientAsync()).StopServerAsync();

      BusyWith = "RUN AND INSPECT";
      EnableBusyCancel = true;
      IsBusy = true;

      var managedImport = new ManagedImport(PathToRFile, _simLibrary);

      string? error = default;

      try
      {
        await managedImport.InspectAsync(await serverLicense.GetRClientAsync());
        UnaryFuncs = managedImport.UnaryFunctions;
        Scalars = managedImport.Scalars;
        ScalarSets = managedImport.ScalarSets;
        DataSets = managedImport.DataSets;
        ManagedImport = managedImport;
      }
      catch (Exception ex)
      {
        error = ex.Message;
      }
      finally
      {
        IsBusy = false;
        serverLicense.Dispose();
        _busyCancelHandler = null;
      }

      if (error != default)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(ImportSimulationViewModel),
          nameof(RunInspectionAsync),
          error,
          this
          );
      }
    }

    private void HandleSelectExec()
    {
      RequireNotNull(ManagedImport);

      var unaryFunctionSelectedIndex = ManagedImport.UnaryFunctions
        .FindIndex(uf => uf == ManagedImport.ExecutorFunction);
      var scalarSetSelectedIndex = ManagedImport.ScalarSets
        .FindIndex(sv => sv == ManagedImport.ExecutorFormal);

      var selectExecViewModel = new SelectExecViewModel(
        ManagedImport.UnaryFunctions.Map(uf => uf.Symbol.AssertNotNull()),
        unaryFunctionSelectedIndex,
        ManagedImport.ScalarSets.Map(sv => sv.Symbol.AssertNotNull()),
        scalarSetSelectedIndex
        );

      var okayed = _appService.ShowDialog(selectExecViewModel, default);

      if (!okayed) return;

      var noChange =
        unaryFunctionSelectedIndex == selectExecViewModel.UnaryFunctionSelectedIndex &&
        scalarSetSelectedIndex == selectExecViewModel.ScalarSetSelectedIndex;

      if (noChange) return;

      void Some(ServerLicense serverLicense)
      {
        var function = ManagedImport.UnaryFunctions[selectExecViewModel.UnaryFunctionSelectedIndex];
        var formal = ManagedImport.ScalarSets[selectExecViewModel.ScalarSetSelectedIndex];
        var _ = SetExecAsync(function, formal, serverLicense);
      }
      void None()
      {
        _appService.Notify(
          NotificationType.Information,
          nameof(ImportSimulationViewModel),
          nameof(HandleSelectExec),
          "No R server available",
          this
          );
      }
      _appService.RVisServerPool.RequestServer().Match(Some, None);
    }

    private async Task SetExecAsync(ISymbolInfo function, ISymbolInfo formal, ServerLicense serverLicense)
    {
      RequireNotNull(ManagedImport);

      _busyCancelHandler = async () => await (await serverLicense.GetRClientAsync()).StopServerAsync();

      BusyWith = "RUN EXECUTIVE FUNCTION";
      EnableBusyCancel = true;
      IsBusy = true;

      string? error = default;

      try
      {
        await ManagedImport.SetExecutorAsync(function, formal, await serverLicense.GetRClientAsync());
        ExecutiveFunction = ManagedImport.ExecutorFunction;
        ExecutiveFormal = ManagedImport.ExecutorFormal;
        ExecutiveOutput = ManagedImport.ExecutorOutput;
      }
      catch (Exception ex)
      {
        error = ex.Message;
      }
      finally
      {
        IsBusy = false;
        serverLicense.Dispose();
        _busyCancelHandler = null;
      }

      if (error != default)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(ImportSimulationViewModel),
          nameof(SetExecAsync),
          error,
          this
          );
      }
    }

    private async Task DoImportUsingExecAsync(ServerLicense serverLicense)
    {
      RequireNotNull(ManagedImport);

      _busyCancelHandler = async () => await (await serverLicense.GetRClientAsync()).StopServerAsync();

      BusyWith = "IMPORT EXEC";
      EnableBusyCancel = true;
      IsBusy = true;

      string? error = default;

      try
      {
        var destinationDirectoryName =
          await ManagedImport.ImportExecToLibraryAsync(
            await serverLicense.GetRClientAsync()
            );

        _simLibrary.Refresh();
        ResetInspection();
        PathToRFile = default;

        var fileName = Path.GetFileName(PathToRFile);
        _appState.Status = $"Imported {fileName} into {destinationDirectoryName}";
      }
      catch (Exception ex)
      {
        error = ex.Message;
      }
      finally
      {
        IsBusy = false;
        serverLicense.Dispose();
        _busyCancelHandler = null;
      }

      if (error != default)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(ImportSimulationViewModel),
          nameof(DoImportUsingExecAsync),
          error,
          this
          );
      }
    }

    private void HandleImportUsingExec()
    {
      RequireNotNull(ManagedImport);
      RequireNotNull(ManagedImport.ExecutorIndependentVariable);

      var importExecViewModel = new ImportExecViewModel(ManagedImport, _appService);
      var didOK = _appService.ShowDialog(importExecViewModel, default);

      if (didOK)
      {
        foreach (var vm in importExecViewModel.ParameterCandidates)
        {
          var candidate = ManagedImport.ExecutorParameterCandidates
            .Find(pc => pc.Name == vm.Name)
            .AssertSome();
          candidate.IsUsed = vm.IsUsed;
          candidate.Unit = vm.Unit;
          candidate.Description = vm.Description;
        }

        var ivCandidate = ManagedImport.ExecutorIndependentVariable.ElementCandidates[0];
        ivCandidate.Unit = importExecViewModel.IndependentVariable.Unit;
        ivCandidate.Description = importExecViewModel.IndependentVariable.Description;

        foreach (var vm in importExecViewModel.ElementCandidates)
        {
          var valueCandidate = ManagedImport.ExecutorValueCandidates
            .Find(pc => pc.Name == vm.Name)
            .AssertSome();
          var candidate = valueCandidate.ElementCandidates[0];
          candidate.IsUsed = vm.IsUsed;
          candidate.Unit = vm.Unit;
          candidate.Description = vm.Description;
        }

        ManagedImport.SimulationName = importExecViewModel.SimulationName;
        ManagedImport.SimulationDescription = importExecViewModel.SimulationDescription;

        void Some(ServerLicense serverLicense)
        {
          var _ = DoImportUsingExecAsync(serverLicense);
        }

        void None()
        {
          _appService.Notify(
            NotificationType.Information,
            nameof(ImportSimulationViewModel),
            nameof(HandleImportUsingExec),
            "No R server available",
            this
            );
        }

        _appService.RVisServerPool.RequestServer().Match(Some, None);
      }
    }

    private async Task DoImportUsingTmplAsync(ServerLicense serverLicense)
    {
      RequireNotNull(ManagedImport);

      _busyCancelHandler = async () => await (await serverLicense.GetRClientAsync()).StopServerAsync();

      BusyWith = "IMPORT TMPL";
      EnableBusyCancel = true;
      IsBusy = true;

      string? error = default;

      try
      {
        var destinationDirectoryName =
          await ManagedImport.ImportTmplToLibraryAsync(
            await serverLicense.GetRClientAsync()
            );

        _simLibrary.Refresh();
        ResetInspection();
        PathToRFile = default;

        var fileName = Path.GetFileName(PathToRFile);
        _appState.Status = $"Imported {fileName} into {destinationDirectoryName}";
      }
      catch (Exception ex)
      {
        error = ex.Message;
      }
      finally
      {
        IsBusy = false;
        serverLicense.Dispose();
        _busyCancelHandler = null;
      }

      if (error != default)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(ImportSimulationViewModel),
          nameof(DoImportUsingTmplAsync),
          error,
          this
          );
      }
    }

    private void HandleImportUsingTmpl()
    {
      RequireNotNull(ManagedImport);
      RequireFile(PathToRFile);

      var fileName = Path.GetFileName(PathToRFile);
      var importTmplViewModel = new ImportTmplViewModel(ManagedImport, fileName, _appService);
      var didOK = _appService.ShowDialog(importTmplViewModel, default);

      if (didOK)
      {
        RequireNotNull(importTmplViewModel.IndependentVariable);

        foreach (var vm in importTmplViewModel.ParameterCandidates)
        {
          var candidate = ManagedImport.ParameterCandidates
            .Find(pc => pc.Name == vm.Name)
            .AssertSome();
          candidate.IsUsed = vm.IsUsed;
          candidate.Unit = vm.Unit;
          candidate.Description = vm.Description;
        }

        foreach (var valueCandidate in ManagedImport.ValueCandidates)
        {
          foreach (var elementCandidate in valueCandidate.ElementCandidates)
          {
            elementCandidate.IsUsed = false;
            elementCandidate.IsIndependentVariable = false;
          }
        }

        var ivValueCandidate = ManagedImport.ValueCandidates
          .Find(vc => vc.Name == importTmplViewModel.IndependentVariable.ValueName)
          .AssertSome();
        var ivElementCandidate = ivValueCandidate.ElementCandidates
          .Find(ec => ec.Name == importTmplViewModel.IndependentVariable.Name)
          .AssertSome();
        ivElementCandidate.IsUsed = true;
        ivElementCandidate.Unit = importTmplViewModel.IndependentVariable.Unit;
        ivElementCandidate.Description = importTmplViewModel.IndependentVariable.Description;
        ivElementCandidate.IsIndependentVariable = true;

        foreach (var vm in importTmplViewModel.ElementCandidates)
        {
          var valueCandidate = ManagedImport.ValueCandidates
            .Find(vc => vc.Name == vm.ValueName)
            .AssertSome();
          var elementCandidate = valueCandidate.ElementCandidates
            .Find(ec => ec.Name == vm.Name)
            .AssertSome();
          elementCandidate.IsUsed = vm.IsUsed;
          elementCandidate.Unit = vm.Unit;
          elementCandidate.Description = vm.Description;
        }

        ManagedImport.SimulationName = importTmplViewModel.SimulationName;
        ManagedImport.SimulationDescription = importTmplViewModel.SimulationDescription;

        void Some(ServerLicense serverLicense)
        {
          var _ = DoImportUsingTmplAsync(serverLicense);
        }

        void None()
        {
          _appService.Notify(
            NotificationType.Information,
            nameof(ImportSimulationViewModel),
            nameof(HandleImportUsingTmpl),
            "No R server available",
            this
            );
        }

        _appService.RVisServerPool.RequestServer().Match(Some, None);
      }
    }

    private void HandleBusyCancel() => _busyCancelHandler?.Invoke();

    private void ResetInspection()
    {
      ManagedImport?.Dispose();
      ManagedImport = default;
      UnaryFuncs = default;
      Scalars = default;
      ScalarSets = default;
      DataSets = default;
      ExecutiveFunction = default;
      ExecutiveFormal = default;
      ExecutiveOutput = default;
    }

    private readonly SimLibrary _simLibrary;
    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private Action? _busyCancelHandler;
  }
}
