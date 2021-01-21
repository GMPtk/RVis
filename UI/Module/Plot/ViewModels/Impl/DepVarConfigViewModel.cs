using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using RVisUI.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;

namespace Plot
{
  public sealed class DepVarConfigViewModel : ReactiveObject, IDepVarConfigViewModel, IDisposable
  {
    internal DepVarConfigViewModel(
      SimOutput output,
      ISimEvidence evidence,
      IAppService appService,
      DepVarConfigState depVarConfigState
      )
    {
      RequireNotNull(output);
      RequireTrue(output.SimValues.Count > 0);
      RequireNotNull(evidence);
      RequireNotNull(depVarConfigState);

      var elements = output.DependentVariables;

      RequireFalse(elements.IsEmpty);
      RequireTrue(
        depVarConfigState.SelectedElementName.IsntAString() ||
        elements.Exists(e => e.Name == depVarConfigState.SelectedElementName)
        );

      _elements = elements;
      _evidence = evidence;
      _depVarConfigState = depVarConfigState;

      ToggleView = ReactiveCommand.Create(HandleToggleView);
      _selectElement = ReactiveCommand.Create<ISelectableItemViewModel>(HandleSelectElement);
      _selectSupplementaryElement = ReactiveCommand.Create<ISelectableItemViewModel>(HandleSelectSupplementaryElement);
      _selectObservations = ReactiveCommand.Create<ISelectableItemViewModel>(HandleSelectObservations);

      var elementViewModels = elements
        .Map(e =>
          new SelectableItemViewModel<SimElement>(
            e,
            e.GetFQName(),
            e.Name.ToUpperInvariant(),
            _selectElement,
            false
            )
        )
        .OrderBy(vm => vm.SortKey);

      _elementViewModels = elementViewModels.ToArr();

      depVarConfigState.SelectedElementName ??= elementViewModels.First().Item.Name;

      _selectedElement = elementViewModels.Single(e => e.Item.Name == depVarConfigState.SelectedElementName);

      var partitioned = elementViewModels.Select(
        e => e == _selectedElement
          ? (None, None)
          : depVarConfigState.MRUElementNames.Exists(n => n == e.Item.Name)
            ? (MRU: Some(e), LRU: Option<SelectableItemViewModel<SimElement>>.None)
            : (MRU: None, LRU: Some(e))
        );
      MRUElements = new ObservableCollection<ISelectableItemViewModel>(partitioned.Choose(t => t.MRU));
      LRUElements = new ObservableCollection<ISelectableItemViewModel>(partitioned.Choose(t => t.LRU));

      _isScaleLogarithmic = depVarConfigState.IsScaleLogarithmic;

      InsetOptions = Array("(none)") + _elementViewModels.Map(vm => vm.Item.Name);
      SelectedInsetOption = depVarConfigState.SelectedInsetElementName.IsAString() 
        ? InsetOptions.FindIndex(io => io == depVarConfigState.SelectedElementName) + 1 
        : 0;

      PopulateSupplementaryElements();
      PopulateObservations();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        evidence.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations>, ObservableQualifier)>(
            ObserveEvidenceObservationChange
            )
          ),

        depVarConfigState
          .ObservableForProperty(s => s.SelectedElementName)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveStateSelectedElementName)
            ),

        depVarConfigState
          .ObservableForProperty(s => s.ObservationsReferences)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveStateObservationsReferences)
            ),

        this
          .ObservableForProperty(vm => vm.IsScaleLogarithmic)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveIsScaleLogarithmic)
            ),

        this
          .ObservableForProperty(vm => vm.SelectedInsetOption)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveSelectedInsetOption)
            )
          
        );
    }

    public ICommand ToggleView { get; }

    public bool IsViewOpen
    {
      get => _isViewOpen;
      set => this.RaiseAndSetIfChanged(ref _isViewOpen, value);
    }
    private bool _isViewOpen;

    public ISelectableItemViewModel SelectedElement
    {
      get => _selectedElement;
      set => this.RaiseAndSetIfChanged(ref _selectedElement, value);
    }
    private ISelectableItemViewModel _selectedElement;

    public ObservableCollection<ISelectableItemViewModel> MRUElements { get; }

    public ObservableCollection<ISelectableItemViewModel> LRUElements { get; }

    public Arr<string> InsetOptions
    {
      get => _insetOptions;
      set => this.RaiseAndSetIfChanged(ref _insetOptions, value);
    }
    private Arr<string> _insetOptions;

    public int SelectedInsetOption 
    { 
      get => _selectedInsetOption; 
      set => this.RaiseAndSetIfChanged(ref _selectedInsetOption, value); 
    }
    private int _selectedInsetOption;

    public Arr<ISelectableItemViewModel> SupplementaryElements
    {
      get => _supplementaryElements;
      set => this.RaiseAndSetIfChanged(ref _supplementaryElements, value);
    }
    private Arr<ISelectableItemViewModel> _supplementaryElements;

    public Arr<ISelectableItemViewModel> Observations
    {
      get => _observations;
      set => this.RaiseAndSetIfChanged(ref _observations, value);
    }
    private Arr<ISelectableItemViewModel> _observations;

    public bool IsScaleLogarithmic
    {
      get => _isScaleLogarithmic;
      set => this.RaiseAndSetIfChanged(ref _isScaleLogarithmic, value);
    }
    private bool _isScaleLogarithmic;

    public void Dispose() => Dispose(true);

    private void HandleToggleView()
    {
      IsViewOpen = !IsViewOpen;
    }

    private void SetSelectedElement(ISelectableItemViewModel viewModel)
    {
      if (MRUElements.Contains(viewModel)) MRUElements.Remove(viewModel);
      else if (LRUElements.Contains(viewModel)) LRUElements.Remove(viewModel);
      else return;

      MRUElements.Insert(0, SelectedElement);

      SelectedElement = viewModel;

      while (MRUElements.Count > MAX_MRU_COUNT)
      {
        var lruViewModel = MRUElements[^1];
        MRUElements.RemoveAt(MRUElements.Count - 1);
        LRUElements.InsertInOrdered(lruViewModel, vm => vm.SortKey);
      }

      _depVarConfigState.SelectedElementName = _SelectedElement.Item.Name;
      _depVarConfigState.MRUElementNames = MRUElements
        .Cast<SelectableItemViewModel<SimElement>>()
        .Select(vm => vm.Item.Name)
        .ToArr();

      PopulateSupplementaryElements();
      PopulateObservations();
    }

    private void HandleSelectElement(ISelectableItemViewModel viewModel)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        SetSelectedElement(viewModel);
      }
    }

    private void HandleSelectSupplementaryElement(ISelectableItemViewModel viewModel)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var element = ((SelectableItemViewModel<SimElement>)viewModel).Item;

        if (viewModel.IsSelected)
        {
          RequireFalse(_depVarConfigState.SupplementaryElementNames.Contains(element.Name));
          _depVarConfigState.SupplementaryElementNames = _depVarConfigState.SupplementaryElementNames.Add(element.Name);
        }
        else
        {
          RequireTrue(_depVarConfigState.SupplementaryElementNames.Contains(element.Name));
          _depVarConfigState.SupplementaryElementNames = _depVarConfigState.SupplementaryElementNames.Remove(element.Name);
        }
      }
    }

    private void HandleSelectObservations(ISelectableItemViewModel viewModel)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var observations = ((SelectableItemViewModel<SimObservations>)viewModel).Item;
        var reference = _evidence.GetReference(observations);

        if (viewModel.IsSelected)
        {
          RequireFalse(_depVarConfigState.ObservationsReferences.Contains(reference));
          _depVarConfigState.ObservationsReferences = _depVarConfigState.ObservationsReferences.Add(reference);
        }
        else
        {
          RequireTrue(_depVarConfigState.ObservationsReferences.Contains(reference));
          _depVarConfigState.ObservationsReferences = _depVarConfigState.ObservationsReferences.Remove(reference);
        }
      }
    }

    private void ObserveEvidenceObservationChange((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier.IsAdd())
      {
        var element = _SelectedElement.Item;
        var haveObsForSelectedElement = change.Observations.Exists(o => o.Subject == element.Name);
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

    private void ObserveStateSelectedElementName(object _)
    {
      var viewModel = _elementViewModels
        .Find(vm => vm.Item.Name == _depVarConfigState.SelectedElementName)
        .AssertSome($"Unknown element name: {_depVarConfigState.SelectedElementName}");

      SetSelectedElement(viewModel);
    }

    private void ObserveStateObservationsReferences(object _)
    {
      PopulateObservations();
    }

    private void ObserveIsScaleLogarithmic(object _)
    {
      _depVarConfigState.IsScaleLogarithmic = IsScaleLogarithmic;
    }

    private void ObserveSelectedInsetOption(object _)
    {
      _depVarConfigState.SelectedInsetElementName = SelectedInsetOption > 0 
        ? InsetOptions[SelectedInsetOption]
        : null;
    }

    private void PopulateSupplementaryElements()
    {
      var element = _SelectedElement.Item;

      if (element.Unit.IsntAString())
      {
        SupplementaryElements = default;
        return;
      }

      var elementsWithSameUnit = _elements.Filter(
        e => e.Name != element.Name && e.Unit == element.Unit
        );

      var viewModels = elementsWithSameUnit
        .Map(e =>
          new SelectableItemViewModel<SimElement>(
            e,
            e.GetFQName(),
            e.Name.ToUpperInvariant(),
            _selectSupplementaryElement,
            _depVarConfigState.SupplementaryElementNames.Contains(e.Name)
            )
        )
        .OrderBy(vm => vm.SortKey);

      SupplementaryElements = viewModels.ToArr<ISelectableItemViewModel>();
    }

    private void PopulateObservations()
    {
      var element = _SelectedElement.Item;

      var observationSet = _evidence.GetObservationSet(element.Name);

      var selectedObservations = _depVarConfigState.ObservationsReferences
        .Map(r => _evidence.GetObservations(r))
        .Somes()
        .Where(o => o.Subject == element.Name);

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

    private SelectableItemViewModel<SimElement> _SelectedElement =>
      RequireInstanceOf<SelectableItemViewModel<SimElement>>(SelectedElement);

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

    private const int MAX_MRU_COUNT = 3;

    private readonly Arr<SimElement> _elements;
    private readonly ISimEvidence _evidence;
    private readonly DepVarConfigState _depVarConfigState;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private readonly ICommand _selectElement;
    private readonly ICommand _selectSupplementaryElement;
    private readonly ICommand _selectObservations;
    private readonly Arr<SelectableItemViewModel<SimElement>> _elementViewModels;
    private bool _disposed = false;
  }
}
