using Evidence.Controls.Dialogs;
using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace Evidence
{
  internal sealed class ViewModel : IViewModel, ISharedStateProvider, ICommonConfiguration, IDisposable
  {
    internal ViewModel(IAppState appState, IAppService appService, IAppSettings appSettings)
    {
      _appState = appState;
      _appService = appService;
      _simulation = appState.Target.AssertSome("No simulation");
      _evidence = appState.SimEvidence;

      Import = ReactiveCommand.Create(HandleImport);

      _moduleState = ModuleState.LoadOrCreate(_simulation, _evidence);

      _browseViewModel = new BrowseViewModel(appState, appSettings, appService, _moduleState);
      _manageViewModel = new ManageViewModel(appState, appService, _moduleState);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(
        _moduleState.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations>, ObservableQualifier)>(
            ObserveModuleStateObservationsChange
            )
          ),
        _appState.SimSharedState.ObservationsSharedStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservationsSharedState>, ObservableQualifier)>(
            ObserveSharedStateObservationsChange
            )
          )
      );
    }

    public IBrowseViewModel BrowseViewModel => _browseViewModel;
    public IManageViewModel ManageViewModel => _manageViewModel;
    public ICommand Import { get; }

    public void ApplyState(
      SimSharedStateApply applyType,
      Arr<(SimParameter, double, double, Option<IDistribution>)> parameterSharedStates,
      Arr<SimElement> elementSharedStates,
      Arr<SimObservations> observationsSharedStates
      )
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        if (applyType.IncludesObservations())
        {
          if (applyType.IsSet())
          {
            _moduleState.UpdateObservations(observationsSharedStates);
          }
          else if (applyType.IsSingle() && observationsSharedStates.Count == 1)
          {
            var single = observationsSharedStates.Head();
            if (!_moduleState.SelectedObservations.ContainsObservations(single))
            {
              _moduleState.SelectObservations(single);
            }
          }
        }
      }
    }

    public void ShareState(ISimSharedStateBuilder sharedStateBuilder)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _moduleState.SelectedObservations.Iter(
          o => sharedStateBuilder.AddObservations(_evidence.GetReference(o))
          );
      }
    }

    bool? ICommonConfiguration.AutoApplyParameterSharedState
    {
      get => _moduleState.AutoApplyParameterSharedState;
      set => _moduleState.AutoApplyParameterSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareParameterSharedState
    {
      get => _moduleState.AutoShareParameterSharedState;
      set => _moduleState.AutoShareParameterSharedState = value;
    }

    bool? ICommonConfiguration.AutoApplyElementSharedState
    {
      get => _moduleState.AutoApplyElementSharedState;
      set => _moduleState.AutoApplyElementSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareElementSharedState
    {
      get => _moduleState.AutoShareElementSharedState;
      set => _moduleState.AutoShareElementSharedState = value;
    }

    bool? ICommonConfiguration.AutoApplyObservationsSharedState
    {
      get => _moduleState.AutoApplyObservationsSharedState;
      set => _moduleState.AutoApplyObservationsSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareObservationsSharedState
    {
      get => _moduleState.AutoShareObservationsSharedState;
      set => _moduleState.AutoShareObservationsSharedState = value;
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          ModuleState.Save(_moduleState, _simulation, _evidence);

          _subscriptions.Dispose();

          _manageViewModel.Dispose();
          _browseViewModel.Dispose();
          _moduleState.Dispose();
        }
        _disposed = true;
      }
    }

    private void HandleImport()
    {
      var importObservationsViewModel =
        new ImportObservationsViewModel(
          _appState,
          _appService
          );

      var ok = _appService.ShowDialog(
        new ImportObservationsDialog(),
        importObservationsViewModel,
        default
        );

      if (ok)
      {
        RequireNotNullEmptyWhiteSpace(importObservationsViewModel.EvidenceName);
        RequireNotNullEmptyWhiteSpace(importObservationsViewModel.RefName);
        RequireNotNullEmptyWhiteSpace(importObservationsViewModel.RefHash);

        var x = importObservationsViewModel.ObservationsColumnViewModels.Head().Observations;

        var observationsSets = importObservationsViewModel.ObservationsColumnViewModels
          .Tail()
          .Filter(vm => vm.Subject != ObservationsColumnViewModel.NO_SELECTION)
          .Map(vm => (vm.Subject, RefName: vm.RefName ?? vm.ColumnName, X: x, Y: vm.Observations))
          .ToArr();

        var evidenceSource = _evidence.AddEvidenceSource(
          importObservationsViewModel.EvidenceName,
          importObservationsViewModel.EvidenceDescription,
          toSet(observationsSets.Map(o => o.Subject)),
          importObservationsViewModel.RefName,
          importObservationsViewModel.RefHash
          );

        _evidence.AddObservations(evidenceSource.ID, observationsSets);
      }
    }

    private void ObserveModuleStateObservationsChange((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      if (_moduleState.AutoShareObservationsSharedState.IsTrue())
      {
        var references = change.Observations.Map(o => _evidence.GetReference(o));

        if (change.ObservableQualifier.IsAdd())
        {
          _appState.SimSharedState.ShareObservationsState(references);
        }
        else if (change.ObservableQualifier.IsRemove())
        {
          _appState.SimSharedState.UnshareObservationsState(references);
        }
      }
    }

    private void ObserveSharedStateObservationsChange((Arr<SimObservationsSharedState> ObservationsSharedStates, ObservableQualifier ObservableQualifier) change)
    {
      if (_moduleState.AutoApplyObservationsSharedState.IsTrue())
      {
        var observations = change.ObservationsSharedStates
          .Map(oss => _evidence.GetObservations(oss.Reference))
          .Somes()
          .ToArr();

        if (change.ObservableQualifier.IsAdd())
        {
          _moduleState.SelectObservations(observations);
        }
        else if (change.ObservableQualifier.IsRemove())
        {
          _moduleState.UnselectObservations(observations);
        }
      }
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly Simulation _simulation;
    private readonly ISimEvidence _evidence;
    private readonly ModuleState _moduleState;
    private readonly BrowseViewModel _browseViewModel;
    private readonly ManageViewModel _manageViewModel;
    private readonly IDisposable _subscriptions;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private bool _disposed = false;
  }
}
