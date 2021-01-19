using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace RVisUI.Mvvm
{
  public class SelectSimulationViewModel : ReactiveObject, ISelectSimulationViewModel
  {
    public SelectSimulationViewModel(
      SimLibrary simLibrary,
      IAppState appState,
      IAppService appService
      )
    {
      _simLibrary = simLibrary;
      _appState = appState;
      _appService = appService;

      SimulationVMs = new ObservableCollection<ISimulationViewModel>();

      simLibrary.Loaded += HandleSimLibraryLoaded;
      simLibrary.Deleted += HandleSimulationDeleted;

      var svmObservable = this.ObservableForProperty(vm => vm.SelectedSimulationVM, svm => null != svm);
      OpenSimulation = ReactiveCommand.Create(HandleOpenSimulation, svmObservable);
      DeleteSimulation = ReactiveCommand.Create(HandleDeleteSimulation, svmObservable);

      var major = _appState.RVersion.Single(t => t.Name == "major").Value;
      var minor = _appState.RVersion.Single(t => t.Name == "minor").Value;
      RVersion = $"{major}.{minor}";
    }

    public ObservableCollection<ISimulationViewModel> SimulationVMs { get; }

    public ISimulationViewModel? SelectedSimulationVM
    {
      get => _selectedSimulationVM;
      set => this.RaiseAndSetIfChanged(ref _selectedSimulationVM, value);
    }
    private ISimulationViewModel? _selectedSimulationVM;

    public string? PathToLibrary
    {
      get => _pathToLibrary;
      set => this.RaiseAndSetIfChanged(ref _pathToLibrary, value);
    }
    private string? _pathToLibrary;

    public ICommand OpenSimulation { get; }

    public ICommand DeleteSimulation { get; }

    public string? RVersion
    {
      get => _rVersion;
      set => this.RaiseAndSetIfChanged(ref _rVersion, value);
    }
    private string? _rVersion;

    private void HandleSimLibraryLoaded(object? sender, System.EventArgs e)
    {
      RequireNotNullEmptyWhiteSpace(_simLibrary.Location);

      while (SimulationVMs.Count > 0)
      {
        SimulationVMs.RemoveAt(SimulationVMs.Count - 1);
      }
      var simulationVMs = _simLibrary.Simulations.Select(s => new SimulationViewModel(s));
      foreach (var simulationVM in simulationVMs)
      {
        SimulationVMs.Add(simulationVM);
      }

      PathToLibrary = _simLibrary.Location.ContractPath();
    }

    private void HandleSimulationDeleted(object? sender, SimulationDeletedEventArgs e)
    {
      var simulationVM = SimulationVMs.Single(vm => vm.Simulation == e.Simulation);
      if (simulationVM == SelectedSimulationVM) SelectedSimulationVM = default;
      SimulationVMs.Remove(simulationVM);
    }

    private void HandleOpenSimulation() =>
      _appState.Target = Some(_selectedSimulationVM.AssertNotNull().Simulation);

    private void HandleDeleteSimulation()
    {
      var confirmed = _appService.AskUserYesNoQuestion("Sure?", "Delete simulation", "Simulation Library");
      if (confirmed)
      {
        try
        {
          _simLibrary.Delete(SelectedSimulationVM.AssertNotNull().Simulation);
        }
        catch (Exception ex)
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(SelectSimulationViewModel),
            nameof(HandleDeleteSimulation),
            $"Failed ({ex.Message}). Close any open files and try again."
            );
        }
      }
    }

    private readonly SimLibrary _simLibrary;
    private readonly IAppState _appState;
    private readonly IAppService _appService;
  }
}
