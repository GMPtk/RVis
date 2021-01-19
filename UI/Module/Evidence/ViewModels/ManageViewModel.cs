using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows.Input;
using static RVis.Base.Check;

namespace Evidence
{
  internal sealed class ManageViewModel : IManageViewModel, INotifyPropertyChanged, IDisposable
  {
    internal ManageViewModel(IAppState appState, IAppService appService, ModuleState moduleState)
    {
      _evidence = appState.SimEvidence;
      _sharedState = appState.SimSharedState;
      _moduleState = moduleState;

      DeleteEvidenceSource = ReactiveCommand.Create(
        HandleDeleteEvidenceSource,
        this.WhenAny(
          vm => vm.SelectedEvidenceSourceViewModel,
          _ => SelectedEvidenceSourceViewModel != default
          )
        );

      DeleteObservations = ReactiveCommand.Create(
        HandleDeleteObservations,
        this.WhenAny(
          vm => vm.SelectedObservationsViewModel,
          _ => SelectedObservationsViewModel != default
          )
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(
        this
          .ObservableForProperty(vm => vm.SelectedEvidenceSourceViewModel)
          .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveSelectedEvidenceSourceViewModel)),
        _moduleState
          .ObservableForProperty(ms => ms.SelectedEvidenceSource)
          .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveModuleStateSelectedEvidenceSource)),
        _evidence.EvidenceSourcesChanges
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<(SimEvidenceSource, ObservableQualifier)>(ObserveEvidenceSourceChanges)
            )
        );

      EvidenceSourceViewModels = new ObservableCollection<IEvidenceSourceViewModel>(
        _evidence.EvidenceSources.Map(
          es => new EvidenceSourceViewModel(es.ID, es.Name, es.Description)
          )
        );
    }

    private void HandleDeleteEvidenceSource()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        RequireNotNull(SelectedEvidenceSourceViewModel);

        SelectedObservationsViewModel = default;
        ObservationsViewModels = default;

        var id = SelectedEvidenceSourceViewModel.ID;
        SelectedEvidenceSourceViewModel = default;

        var observations = _evidence.GetObservations(id);
        var references = observations.Map(o => _evidence.GetReference(o));

        _evidence.RemoveEvidenceSource(id);

        var index = EvidenceSourceViewModels.FindIndex(vm => vm.ID == id);
        RequireTrue(index.IsFound());
        EvidenceSourceViewModels.RemoveAt(index);

        _sharedState.UnshareObservationsState(references);
      }
    }

    private void HandleDeleteObservations()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        RequireNotNull(SelectedEvidenceSourceViewModel);
        RequireNotNull(SelectedObservationsViewModel);
        RequireNotNull(ObservationsViewModels);

        var esObservations = _evidence.GetObservations(SelectedEvidenceSourceViewModel.ID);
        var observations = esObservations
          .Find(o => o.ID == SelectedObservationsViewModel.ID)
          .AssertSome();
        SelectedObservationsViewModel = default;
        var reference = _evidence.GetReference(observations);
        _evidence.RemoveObservations(observations);

        var index = ObservationsViewModels.FindIndex(vm => vm.ID == observations.ID);
        RequireTrue(index.IsFound());
        ObservationsViewModels.RemoveAt(index);

        _sharedState.UnshareObservationsState(reference);
      }
    }

    private void ObserveSelectedEvidenceSourceViewModel(object _)
    {
      PopulateObservations();

      RequireNotNull(SelectedEvidenceSourceViewModel);

      _moduleState.SelectedEvidenceSource = _evidence.EvidenceSources.Find(
        es => es.ID == SelectedEvidenceSourceViewModel.ID
        );
    }

    private void ObserveModuleStateSelectedEvidenceSource(object _)
    {
      var evidenceSourceViewModel = _moduleState.SelectedEvidenceSource.MatchUnsafe(
        es => EvidenceSourceViewModels.Find(vm => vm.ID == es.ID).AssertSome(),
        default(IEvidenceSourceViewModel)
        );
      SelectedEvidenceSourceViewModel = evidenceSourceViewModel;
      PopulateObservations();
    }

    private void ObserveEvidenceSourceChanges((SimEvidenceSource SimEvidenceSource, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier.IsAdd())
      {
        var evidenceSourceViewModel = new EvidenceSourceViewModel(
          change.SimEvidenceSource.ID,
          change.SimEvidenceSource.Name,
          change.SimEvidenceSource.Description
          );
        EvidenceSourceViewModels.Add(evidenceSourceViewModel);
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        if (SelectedEvidenceSourceViewModel?.ID == change.SimEvidenceSource.ID)
        {
          SelectedObservationsViewModel = default;
          ObservationsViewModels = default;
          SelectedEvidenceSourceViewModel = default;
        }

        var index = EvidenceSourceViewModels.FindIndex(vm => vm.ID == change.SimEvidenceSource.ID);
        RequireTrue(index.IsFound());
        EvidenceSourceViewModels.RemoveAt(index);
      }
    }

    private void PopulateObservations()
    {
      SelectedObservationsViewModel = default;

      if (SelectedEvidenceSourceViewModel == default)
      {
        ObservationsViewModels = default;
        return;
      }

      var observations = _evidence.GetObservations(SelectedEvidenceSourceViewModel.ID);

      var observationsViewModels = new ObservableCollection<IObservationsViewModel>(
        observations.Map(o => new ObservationsViewModel(
          o.ID,
          o.Subject,
          o.RefName,
          SelectedEvidenceSourceViewModel.Name,
          o.X,
          o.Y
          ))
        );

      ObservationsViewModels = observationsViewModels;
    }

    public ObservableCollection<IEvidenceSourceViewModel> EvidenceSourceViewModels { get; }

    public IEvidenceSourceViewModel? SelectedEvidenceSourceViewModel
    {
      get => _selectedEvidenceSourceViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedEvidenceSourceViewModel, value, PropertyChanged);
    }
    private IEvidenceSourceViewModel? _selectedEvidenceSourceViewModel;

    public ICommand DeleteEvidenceSource { get; }

    public ObservableCollection<IObservationsViewModel>? ObservationsViewModels
    {
      get => _observationsViewModels;
      set => this.RaiseAndSetIfChanged(ref _observationsViewModels, value, PropertyChanged);
    }
    private ObservableCollection<IObservationsViewModel>? _observationsViewModels;

    public IObservationsViewModel? SelectedObservationsViewModel
    {
      get => _selectedObservationsViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedObservationsViewModel, value, PropertyChanged);
    }
    private IObservationsViewModel? _selectedObservationsViewModel;

    public ICommand DeleteObservations { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
        }

        _disposed = true;
      }
    }

    private readonly ISimEvidence _evidence;
    private readonly ISimSharedState _sharedState;
    private readonly ModuleState _moduleState;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
