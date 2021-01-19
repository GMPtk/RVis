using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using ReactiveUI;
using RVis.Base;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.LangExt;
using static System.Double;

namespace Evidence
{
  internal sealed class BrowseViewModel : IBrowseViewModel, INotifyPropertyChanged, IDisposable
  {
    internal BrowseViewModel(IAppState appState, IAppSettings appSettings, IAppService appService, ModuleState moduleState)
    {
      _appState = appState;
      _appSettings = appSettings;
      _moduleState = moduleState;

      var evidence = _appState.SimEvidence;

      var simulation = _appState.Target.AssertSome();
      var ivElement = simulation.SimConfig.SimOutput.IndependentVariable;

      _observationsScatterPlot = new PlotModel();
      _observationsScatterPlot.AssignDefaultColors(appSettings.IsBaseDark, PLOT_COLOR_COUNT);
      _observationsScatterPlot.AddAxes(ivElement.GetFQName(), default, default, default, default, default);
      _observationsScatterPlot.ApplyThemeToPlotModelAndAxes();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(
              ObserveAppSettingsPropertyChange
            )
          ),

        _moduleState
          .ObservableForProperty(ms => ms.SelectedObservationsSet)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSelectedObservationSet
            )
          ),

        _moduleState
          .ObservableForProperty(ms => ms.SelectedObservations)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSelectedObservations
            )
          ),

        evidence.EvidenceSourcesChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(SimEvidenceSource, ObservableQualifier)>(
            ObserveEvidenceSourcesChanges
          )
        ),

        evidence.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations>, ObservableQualifier)>(
            ObserveEvidenceObservationsChanges
          )
        ),

        this
          .ObservableForProperty(vm => vm.SelectedSubjectViewModel)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSelectedSubjectViewModel
            )
          )

        );

      _allSubjectViewModels = evidence.Subjects
        .ToArr()
        .Map(s =>
          {
            var nSelected = _moduleState.SelectedObservations.Count(o => o.Subject == s);
            var observationsSet = evidence.GetObservationSet(s);
            var nAvailable = observationsSet.Observations.Count;
            return new SubjectViewModel(s) { NSelected = nSelected, NAvailable = nAvailable };
          });

      PopulateSubjects();

      var maybeSelectedSubjectViewModel = _moduleState.SelectedObservationsSet.Match(
        os => SubjectViewModels.Find(vm => vm.Subject == os.Subject),
        NoneOf<ISubjectViewModel>
        );
      SelectedSubjectViewModel = maybeSelectedSubjectViewModel.IfNoneUnsafe(default(ISubjectViewModel)!);

      PopulateObservations();
      PlotObservations();
    }

    public PlotModel? PlotModel
    {
      get => _plotModel;
      set => this.RaiseAndSetIfChanged(ref _plotModel, value, PropertyChanged);
    }
    private PlotModel? _plotModel;

    public Arr<ISubjectViewModel> SubjectViewModels
    {
      get => _subjectViewModels;
      set => this.RaiseAndSetIfChanged(ref _subjectViewModels, value, PropertyChanged);
    }
    private Arr<ISubjectViewModel> _subjectViewModels;

    public ISubjectViewModel? SelectedSubjectViewModel
    {
      get => _selectedSubjectViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedSubjectViewModel, value, PropertyChanged);
    }
    private ISubjectViewModel? _selectedSubjectViewModel;

    public Arr<IObservationsViewModel> ObservationsViewModels
    {
      get => _observationsViewModels;
      set => this.RaiseAndSetIfChanged(ref _observationsViewModels, value, PropertyChanged);
    }
    private Arr<IObservationsViewModel> _observationsViewModels;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _observationsViewModelIsSelectedSubscriptions?.Dispose();
          _subscriptions.Dispose();
        }

        _disposed = true;
      }
    }

    private void PopulateSubjects()
    {
      var subjectViewModelsWithDataAvailable = _allSubjectViewModels
        .Filter(vm => vm.NAvailable > 0)
        .OrderBy(vm => vm.Subject.ToUpperInvariant())
        .ToArr<ISubjectViewModel>();
      if (SelectedSubjectViewModel is null || !subjectViewModelsWithDataAvailable.Contains(SelectedSubjectViewModel))
      {
        SelectedSubjectViewModel = default;
      }
      SubjectViewModels = subjectViewModelsWithDataAvailable;
    }

    private void PopulateObservations()
    {
      _observationsViewModelIsSelectedSubscriptions?.Dispose();
      _observationsViewModelIsSelectedSubscriptions = null;

      if (SelectedSubjectViewModel == default)
      {
        ObservationsViewModels = default;
        return;
      }

      var observationsSet = _appState.SimEvidence.GetObservationSet(SelectedSubjectViewModel.Subject);
      var observationsViewModels = observationsSet.Observations.Map(o =>
      {
        var evidenceSource = _appState.SimEvidence.EvidenceSources
          .Find(es => es.ID == o.EvidenceSourceID)
          .AssertSome();

        var isSelected = _moduleState.SelectedObservations.ContainsObservations(o);

        return new ObservationsViewModel(o.ID, o.Subject, o.RefName, evidenceSource.Name, o.X, o.Y)
        {
          IsSelected = isSelected
        }
        as IObservationsViewModel;
      });

      var onNextHandler = _reactiveSafeInvoke.SuspendAndInvoke<IObservedChange<IObservationsViewModel, bool>>(
        ObserveObservationsViewModelIsSelected
        );

      var disposables = observationsViewModels.Map(
        ovm => ovm.ObservableForProperty(vm => vm.IsSelected).Subscribe(onNextHandler)
        );
      _observationsViewModelIsSelectedSubscriptions = new CompositeDisposable(disposables);

      ObservationsViewModels = observationsViewModels;
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      _observationsScatterPlot.AssignDefaultColors(_appSettings.IsBaseDark, PLOT_COLOR_COUNT);
      _observationsScatterPlot.ApplyThemeToPlotModelAndAxes();
      _observationsScatterPlot.InvalidatePlot(true);
    }

    private void ObserveModuleStateSelectedObservationSet(object _)
    {
      var maybeSubjectViewModel =
        from os in _moduleState.SelectedObservationsSet
        from vm in SubjectViewModels.Find(vm => vm.Subject == os.Subject)
        select vm;

      var subjectViewModel = maybeSubjectViewModel.IfNone(default(ISubjectViewModel));

      if (SelectedSubjectViewModel != subjectViewModel)
      {
        SelectedSubjectViewModel = subjectViewModel;
        PopulateObservations();
        PlotObservations();
      }
    }

    private void ObserveModuleStateSelectedObservations(object _)
    {
      var counts = _moduleState.SelectedObservations
        .GroupBy(o => o.Subject)
        .ToDictionary(g => g.Key, g => g.Count());

      foreach (var subjectViewModel in _allSubjectViewModels)
      {
        subjectViewModel.NSelected = counts.TryGetValue(subjectViewModel.Subject, out int count)
          ? count
          : 0;
      }

      PopulateSubjects();
      PopulateObservations();
      PlotObservations();
    }

    private void ObserveEvidenceSourcesChanges((SimEvidenceSource EvidenceSource, ObservableQualifier ObservableQualifier) change)
    {
      change.EvidenceSource.Subjects.Iter(UpdateSubject);
    }

    private void UpdateSubject(string subject)
    {
      var subjectViewModel = _allSubjectViewModels
        .Find(vm => vm.Subject == subject)
        .AssertSome();
      var observationsSet = _appState.SimEvidence.GetObservationSet(subject);
      var nAvailable = observationsSet.Observations.Count;
      subjectViewModel.NAvailable = nAvailable;
    }

    private void ObserveEvidenceObservationsChanges((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      var bySubject = change.Observations.GroupBy(o => o.Subject);
      bySubject.Iter(g => UpdateSubject(g.Key));

      PopulateSubjects();
      PopulateObservations();
      PlotObservations();
    }

    private void ObserveSelectedSubjectViewModel(object _)
    {
      _moduleState.SelectedObservationsSet = SelectedSubjectViewModel == default
        ? None
        : Some(_appState.SimEvidence.GetObservationSet(SelectedSubjectViewModel.Subject));

      PopulateObservations();
      PlotObservations();
    }

    private void ObserveObservationsViewModelIsSelected(IObservedChange<IObservationsViewModel, bool> change)
    {
      RequireNotNull(SelectedSubjectViewModel);

      var observationsViewModel = change.Sender;
      var observationsSet = _appState.SimEvidence.GetObservationSet(SelectedSubjectViewModel.Subject);
      var observations = observationsSet.Observations.Find(o => o.ID == observationsViewModel.ID).AssertSome();
      if (observationsViewModel.IsSelected)
      {
        _moduleState.SelectObservations(observations);
      }
      else
      {
        _moduleState.UnselectObservations(observations);
      }

      var nSelected = _moduleState.SelectedObservations.Count(o => o.Subject == SelectedSubjectViewModel.Subject);
      SelectedSubjectViewModel.NSelected = nSelected;

      PlotObservations();
    }

    private void PlotObservations()
    {
      var selectedObservationViewModels = ObservationsViewModels.Filter(vm => vm.IsSelected);

      if (selectedObservationViewModels.IsEmpty)
      {
        PlotModel = default;
        return;
      }

      RequireNotNull(SelectedSubjectViewModel);

      var verticalAxis = _observationsScatterPlot.GetAxis(AxisPosition.Left).AssertNotNull();
      if (!SelectedSubjectViewModel.Subject.Equals(verticalAxis.Tag))
      {
        var simulation = _appState.Target.AssertSome();
        var output = simulation.SimConfig.SimOutput;
        var dvElement = output.FindElement(SelectedSubjectViewModel.Subject).AssertSome();
        verticalAxis.Title = dvElement.GetFQName();
        verticalAxis.Tag = SelectedSubjectViewModel.Subject;

        _observationsScatterPlot.Series.Clear();
      }

      var observationsSet = _appState.SimEvidence.GetObservationSet(SelectedSubjectViewModel.Subject);

      var selectedObservations = selectedObservationViewModels.Map(
        vm => observationsSet.Observations.Find(o => o.ID == vm.ID).AssertSome()
        );

      var xMin = selectedObservations.Min(o => o.X.Min());
      var xMax = selectedObservations.Max(o => o.X.Max());

      var yMin = selectedObservations.Min(o => o.Y.Min());
      var yMax = selectedObservations.Max(o => o.Y.Max());

      var horizontalAxisMin = xMax > 0.0 && xMin > 0.0 && (xMax - xMin) > xMin
        ? 0.0
        : NaN;

      var verticalAxisMin = yMax > 0.0 && yMin > 0.0 && (yMax - yMin) > yMin
        ? 0.0
        : NaN;

      verticalAxis.Minimum = verticalAxisMin;
      var horizontalAxis = _observationsScatterPlot.GetAxis(AxisPosition.Bottom).AssertNotNull();
      horizontalAxis.Minimum = horizontalAxisMin;

      _observationsScatterPlot.Series.Clear();

      selectedObservations.Iter((i, o) => _observationsScatterPlot.AddScatterSeries(
        i,
        o.RefName,
        o.X,
        o.Y,
        _observationsScatterPlot.DefaultColors,
        default,
        o.ID
        ));

      _observationsScatterPlot.ResetAllAxes();
      _observationsScatterPlot.InvalidatePlot(true);

      PlotModel = _observationsScatterPlot;
    }

    private const int PLOT_COLOR_COUNT = 8;

    private readonly IAppState _appState;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly PlotModel _observationsScatterPlot;
    private readonly Arr<SubjectViewModel> _allSubjectViewModels;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private IDisposable? _observationsViewModelIsSelectedSubscriptions;
    private bool _disposed = false;
  }
}
