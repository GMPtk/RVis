using LanguageExt;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static LanguageExt.Prelude;

namespace Evidence
{
  public sealed class ModuleState : INotifyPropertyChanged, IDisposable
  {
    private class _ModuleStateDTO
    {
      public string[]? SelectedObservationsReferences { get; set; }
      public string? SelectedSubject { get; set; }
      public string? SelectedEvidenceSourceReference { get; set; }
      public bool? AutoApplyParameterSharedState { get; set; }
      public bool? AutoShareParameterSharedState { get; set; }
      public bool? AutoApplyElementSharedState { get; set; }
      public bool? AutoShareElementSharedState { get; set; }
      public bool? AutoApplyObservationsSharedState { get; set; } = false;
      public bool? AutoShareObservationsSharedState { get; set; } = true;
    }

    public static void Save(ModuleState instance, Simulation simulation, ISimEvidence evidence)
    {
      simulation.SavePrivateData(
        new _ModuleStateDTO
        {
          SelectedObservationsReferences = instance.SelectedObservations
            .Map(o => evidence.GetReference(o))
            .ToArray(),
          SelectedSubject = instance.SelectedObservationsSet
            .MatchUnsafe<string?>(os => os.Subject, () => default),
          SelectedEvidenceSourceReference = instance.SelectedEvidenceSource
            .MatchUnsafe<string?>(es => es.GetReference(), () => default),
          AutoApplyParameterSharedState = instance.AutoApplyParameterSharedState,
          AutoShareParameterSharedState = instance.AutoShareParameterSharedState,
          AutoApplyElementSharedState = instance.AutoApplyElementSharedState,
          AutoShareElementSharedState = instance.AutoShareElementSharedState,
          AutoApplyObservationsSharedState = instance.AutoApplyObservationsSharedState,
          AutoShareObservationsSharedState = instance.AutoShareObservationsSharedState
        },
        nameof(Evidence),
        nameof(ViewModel),
        nameof(ModuleState)
        );
    }

    public static ModuleState LoadOrCreate(Simulation simulation, ISimEvidence evidence)
    {
      var maybeDTO = simulation.LoadPrivateData<_ModuleStateDTO>(
        nameof(Evidence),
        nameof(ViewModel),
        nameof(ModuleState)
        );

      return maybeDTO.Match(
        dto => new ModuleState(dto, evidence),
        () => new ModuleState(evidence)
        );
    }

    private ModuleState(_ModuleStateDTO dto, ISimEvidence evidence)
    {
      _selectedObservations = dto.SelectedObservationsReferences.IsNullOrEmpty()
        ? default
        : dto.SelectedObservationsReferences
            .Select(r => evidence.GetObservations(r))
            .Somes()
            .ToArr();

      _selectedObservationsSet = evidence.IsSubject(dto.SelectedSubject)
        ? Some(evidence.GetObservationSet(dto.SelectedSubject))
        : None;

      _selectedEvidenceSource = dto.SelectedEvidenceSourceReference.IsAString()
        ? evidence.GetEvidenceSource(dto.SelectedEvidenceSourceReference)
        : None;

      _autoApplyParameterSharedState = dto.AutoApplyParameterSharedState;
      _autoShareParameterSharedState = dto.AutoShareParameterSharedState;
      _autoApplyElementSharedState = dto.AutoApplyElementSharedState;
      _autoShareElementSharedState = dto.AutoShareElementSharedState;
      _autoApplyObservationsSharedState = dto.AutoApplyObservationsSharedState;
      _autoShareObservationsSharedState = dto.AutoShareObservationsSharedState;

      Subscribe(evidence);
    }

    private ModuleState(ISimEvidence evidence) : this(new _ModuleStateDTO(), evidence)
    {
    }

    public void SelectObservations(SimObservations observations) =>
      SelectObservations(Array(observations));

    public void SelectObservations(Arr<SimObservations> observations)
    {
      var toSelect = observations.Filter(o => !SelectedObservations.ContainsObservations(o));
      if (toSelect.IsEmpty) return;

      SelectedObservations = SelectedObservations.AddRange(toSelect);
      _observationsChangesSubject.OnNext((toSelect, ObservableQualifier.Add));
    }

    public void UnselectObservations(SimObservations observations) =>
      UnselectObservations(Array(observations));

    public void UnselectObservations(Arr<SimObservations> observations)
    {
      var toUnselect = observations.Filter(o => SelectedObservations.ContainsObservations(o));
      if (toUnselect.IsEmpty) return;

      SelectedObservations = SelectedObservations.Filter(o => !observations.ContainsObservations(o));
      _observationsChangesSubject.OnNext((toUnselect, ObservableQualifier.Remove));
    }

    public void UpdateObservations(Arr<SimObservations> observations)
    {
      var added = observations.Filter(o => !SelectedObservations.ContainsObservations(o));
      var removed = observations.Filter(o => SelectedObservations.ContainsObservations(o));

      SelectedObservations = observations;

      if (!added.IsEmpty)
      {
        _observationsChangesSubject.OnNext((added, ObservableQualifier.Add));
      }

      if (!removed.IsEmpty)
      {
        _observationsChangesSubject.OnNext((removed, ObservableQualifier.Remove));
      }
    }

    public Arr<SimObservations> SelectedObservations
    {
      get => _selectedObservations;
      private set => this.RaiseAndSetIfChanged(ref _selectedObservations, value, PropertyChanged);
    }
    private Arr<SimObservations> _selectedObservations;

    public IObservable<(Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier)> ObservationsChanges =>
      _observationsChangesSubject.AsObservable();
    private readonly ISubject<(Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier)> _observationsChangesSubject =
      new Subject<(Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier)>();

    public Option<SimObservationsSet> SelectedObservationsSet
    {
      get => _selectedObservationsSet;
      set => this.RaiseAndSetIfChanged(ref _selectedObservationsSet, value, PropertyChanged);
    }
    private Option<SimObservationsSet> _selectedObservationsSet;

    public Option<SimEvidenceSource> SelectedEvidenceSource
    {
      get => _selectedEvidenceSource;
      set => this.RaiseAndSetIfChanged(ref _selectedEvidenceSource, value, PropertyChanged);
    }
    private Option<SimEvidenceSource> _selectedEvidenceSource;

    public bool? AutoApplyParameterSharedState
    {
      get => _autoApplyParameterSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyParameterSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyParameterSharedState;

    public bool? AutoShareParameterSharedState
    {
      get => _autoShareParameterSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareParameterSharedState, value, PropertyChanged);
    }
    private bool? _autoShareParameterSharedState;

    public bool? AutoApplyElementSharedState
    {
      get => _autoApplyElementSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyElementSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyElementSharedState;

    public bool? AutoShareElementSharedState
    {
      get => _autoShareElementSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareElementSharedState, value, PropertyChanged);
    }
    private bool? _autoShareElementSharedState;

    public bool? AutoApplyObservationsSharedState
    {
      get => _autoApplyObservationsSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyObservationsSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyObservationsSharedState;

    public bool? AutoShareObservationsSharedState
    {
      get => _autoShareObservationsSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareObservationsSharedState, value, PropertyChanged);
    }
    private bool? _autoShareObservationsSharedState;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions?.Dispose();
          _subscriptions = default;
        }

        _disposed = true;
      }
    }

    private void Subscribe(ISimEvidence evidence)
    {
      _subscriptions = new CompositeDisposable(
        evidence.EvidenceSourcesChanges.Subscribe(ObserveEvidenceSourcesChanges),
        evidence.ObservationsChanges.Subscribe(ObserveEvidenceObservationsChanges)
        );
    }

    private void ObserveEvidenceSourcesChanges((SimEvidenceSource EvidenceSource, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier.IsRemove())
      {
        var isSelected = SelectedEvidenceSource.Match(
          es => es.ID == change.EvidenceSource.ID,
          () => false
          );

        if (isSelected) SelectedEvidenceSource = None;
      }
    }

    private void ObserveEvidenceObservationsChanges((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier.IsRemove())
      {
        var toUnselect = SelectedObservations.Filter(
          o => change.Observations.ContainsObservations(o)
          );

        UnselectObservations(toUnselect);
      }
    }

    private IDisposable? _subscriptions;
    private bool _disposed = false;
  }
}
