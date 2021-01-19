using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;

namespace Sampling
{
  internal sealed class OutputsEvidenceViewModel : IOutputsEvidenceViewModel, INotifyPropertyChanged, IDisposable
  {
    internal OutputsEvidenceViewModel(IAppState appState, IAppService appService, ModuleState moduleState)
    {
      _moduleState = moduleState;
      _evidence = appState.SimEvidence;

      _selectObservations = ReactiveCommand.Create<ISelectableItemViewModel>(HandleSelectObservations);

      PopulateObservations();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _evidence.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations>, ObservableQualifier)>(
            ObserveEvidenceObservationChange
            )
          ),

        _moduleState.OutputsState
          .ObservableForProperty(os => os.SelectedOutputName)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveOutputsStateSelectedOutputName
              )
            ),

        _moduleState.OutputsState
          .ObservableForProperty(os => os.ObservationsReferences)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveOutputsStateObservationsReferences
              )
            )

        );
    }

    public Arr<ISelectableItemViewModel> Observations
    {
      get => _observations;
      set => this.RaiseAndSetIfChanged(ref _observations, value, PropertyChanged);
    }
    private Arr<ISelectableItemViewModel> _observations;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void HandleSelectObservations(ISelectableItemViewModel viewModel)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var observations = ((SelectableItemViewModel<SimObservations>)viewModel).Item;
        var reference = _evidence.GetReference(observations);

        if (viewModel.IsSelected)
        {
          RequireFalse(_moduleState.OutputsState.ObservationsReferences.Contains(reference));
          _moduleState.OutputsState.ObservationsReferences = _moduleState.OutputsState.ObservationsReferences.Add(reference);
        }
        else
        {
          RequireTrue(_moduleState.OutputsState.ObservationsReferences.Contains(reference));
          _moduleState.OutputsState.ObservationsReferences = _moduleState.OutputsState.ObservationsReferences.Remove(reference);
        }
      }
    }

    private void ObserveEvidenceObservationChange((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier.IsAdd())
      {
        var outputName = _moduleState.OutputsState.SelectedOutputName;
        var haveObsForSelectedElement = change.Observations.Exists(o => o.Subject == outputName);
        if (haveObsForSelectedElement) PopulateObservations();
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        var withoutRemoved = Observations
          .Cast<SelectableItemViewModel<SimObservations>>()
          .Where(vm => !change.Observations.ContainsObservations(vm.Item));

        if (withoutRemoved.Count() < Observations.Count)
        {
          Observations = withoutRemoved.ToArr<ISelectableItemViewModel>();
        }
      }
    }

    private void ObserveOutputsStateSelectedOutputName(object _)
    {
      PopulateObservations();
    }

    private void ObserveOutputsStateObservationsReferences(object _)
    {
      PopulateObservations();
    }

    private void PopulateObservations()
    {
      var outputName = _moduleState.OutputsState.SelectedOutputName;

      if (outputName.IsntAString())
      {
        Observations = default;
        return;
      }

      var observationSet = _evidence.GetObservationSet(outputName);

      var selectedObservations = _moduleState.OutputsState.ObservationsReferences
        .Map(r => _evidence.GetObservations(r))
        .Somes()
        .Where(o => o.Subject == outputName);

      var viewModels = observationSet.Observations.Map(
        o => new SelectableItemViewModel<SimObservations>(
          o,
          _evidence.GetFQObservationsName(o), o.ID.ToString(InvariantCulture),
          _selectObservations,
          selectedObservations.Any(p => p.ID == o.ID)
          )
        );

      Observations = viewModels.ToArr<ISelectableItemViewModel>();
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

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

    private readonly ModuleState _moduleState;
    private readonly ISimEvidence _evidence;
    private readonly ICommand _selectObservations;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed;
  }
}
