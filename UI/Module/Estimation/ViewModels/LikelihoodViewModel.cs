using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Double;

namespace Estimation
{
  internal sealed class LikelihoodViewModel : ILikelihoodViewModel, INotifyPropertyChanged, IDisposable
  {
    internal LikelihoodViewModel(IAppState appState, IAppService appService, IAppSettings appSettings, ModuleState moduleState)
    {
      _appSettings = appSettings;
      _simulation = appState.Target.AssertSome("No simulation");
      _evidence = appState.SimEvidence;
      _moduleState = moduleState;

      _toggleSelectOutput = ReactiveCommand.Create<IOutputViewModel>(HandleToggleSelectOutput);

      AllOutputViewModels = _simulation.SimConfig.SimOutput.DependentVariables
        .Map(e => new OutputViewModel(e.Name, _toggleSelectOutput))
        .OrderBy(vm => vm.SortKey)
        .ToArr<IOutputViewModel>();

      _moduleState.OutputStates.Iter(os =>
      {
        var outputViewModel = AllOutputViewModels
          .Find(vm => vm.Name == os.Name)
          .AssertSome($"Unknown output in module state: {os.Name}");

        outputViewModel.IsSelected = os.IsSelected;
        outputViewModel.ErrorModel = os.GetErrorModel().ToString(os.Name);
      });

      SelectedOutputViewModels = new ObservableCollection<IOutputViewModel>(
        AllOutputViewModels.Where(vm => vm.IsSelected)
        );

      SelectedOutputViewModel = SelectedOutputViewModels.FindIndex(
        vm => vm.Name == moduleState.LikelihoodState.SelectedOutput
        );

      var errorModelTypes = ErrorModelType.All;

      _outputErrorViewModel = new OutputErrorViewModel(
        appState,
        appService,
        errorModelTypes
        );

      var ivElement = _simulation.SimConfig.SimOutput.IndependentVariable;
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
          .ObservableForProperty(ms => ms.SelectedObservations)
          .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveModuleStateSelectedObservations
            )
          ),

        _moduleState.OutputStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<OutputState>, ObservableQualifier)>(
            ObserveModuleStateOutputStateChange
            )
          ),

        _evidence.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations>, ObservableQualifier)>(
            ObserveEvidenceObservationsChanges
            )
          ),

        this
          .WhenAny(vm => vm.SelectedOutputViewModel, _ => default(object))
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object?>(
              ObserveSelectedOutputViewModel
              )
            ),

        _outputErrorViewModel
          .ObservableForProperty(vm => vm.OutputState)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveOutputErrorViewModelOutputState
              )
            )

      );

      PopulateObservations();
      PlotObservations();
    }

    public Arr<IOutputViewModel> AllOutputViewModels { get; }

    public ObservableCollection<IOutputViewModel> SelectedOutputViewModels { get; }

    public int SelectedOutputViewModel
    {
      get => _selectedOutputViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputViewModel, value, PropertyChanged);
    }
    private int _selectedOutputViewModel;

    public IOutputErrorViewModel OutputErrorViewModel => _outputErrorViewModel;

    public Arr<IObservationsViewModel> ObservationsViewModels
    {
      get => _observationsViewModels;
      set => this.RaiseAndSetIfChanged(ref _observationsViewModels, value, PropertyChanged);
    }
    private Arr<IObservationsViewModel> _observationsViewModels;

    public PlotModel? PlotModel
    {
      get => _plotModel;
      set => this.RaiseAndSetIfChanged(ref _plotModel, value, PropertyChanged);
    }
    private PlotModel? _plotModel;

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value, PropertyChanged);
    }
    private bool _isVisible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void HandleToggleSelectOutput(IOutputViewModel outputViewModel)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var index = _moduleState.OutputStates.FindIndex(os => os.Name == outputViewModel.Name);
        var isSelected = SelectedOutputViewModels.Contains(outputViewModel);
        OutputState outputState;

        if (index.IsFound())
        {
          outputState = _moduleState.OutputStates[index];
          outputState = outputState.WithIsSelected(!isSelected);
          _moduleState.OutputStates = _moduleState.OutputStates.SetItem(index, outputState);
        }
        else
        {
          outputState = OutputState.Create(outputViewModel.Name);
          _moduleState.OutputStates += outputState;
        }

        if (isSelected)
        {
          SelectedOutputViewModels.Remove(outputViewModel);
          if (SelectedOutputViewModel.IsntFound())
          {
            _outputErrorViewModel.OutputState = None;
            _moduleState.LikelihoodState.SelectedOutput = default;
            PopulateObservations();
            PlotObservations();
          }
        }
        else
        {
          SelectedOutputViewModels.InsertInOrdered(outputViewModel, pvm => pvm.SortKey);
          outputViewModel.ErrorModel = outputState.GetErrorModel().ToString(outputViewModel.Name);
        }

        outputViewModel.IsSelected = !isSelected;
      }
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      _observationsScatterPlot.AssignDefaultColors(_appSettings.IsBaseDark, PLOT_COLOR_COUNT);
      _observationsScatterPlot.ApplyThemeToPlotModelAndAxes();
      _observationsScatterPlot.InvalidatePlot(true);
    }

    private void ObserveModuleStateSelectedObservations(object _)
    {
      PopulateObservations();
      PlotObservations();
    }

    private void ObserveModuleStateOutputStateChange((Arr<OutputState> OutputStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!change.ObservableQualifier.IsAddOrChange()) return;

      var selectedOutputViewModel = SelectedOutputViewModel.IsFound()
        ? SelectedOutputViewModels[SelectedOutputViewModel]
        : default;

      change.OutputStates.Iter(os =>
      {
        var outputViewModel = AllOutputViewModels.Find(vm => vm.Name == os.Name).AssertSome();

        if (os.IsSelected)
        {
          if (!SelectedOutputViewModels.Contains(outputViewModel))
          {
            RequireFalse(outputViewModel.IsSelected);
            outputViewModel.IsSelected = true;
            SelectedOutputViewModels.InsertInOrdered(outputViewModel, pvm => pvm.SortKey);
          }

          outputViewModel.ErrorModel = os.GetErrorModel().ToString(os.Name);

          if (outputViewModel == selectedOutputViewModel)
          {
            OutputErrorViewModel.OutputState = os;
            PopulateObservations();
            PlotObservations();
          }
        }
        else
        {
          if (SelectedOutputViewModels.Contains(outputViewModel))
          {
            RequireTrue(outputViewModel.IsSelected);
            SelectedOutputViewModels.Remove(outputViewModel);
            outputViewModel.IsSelected = false;
          }

          outputViewModel.ErrorModel = default;

          if (outputViewModel == selectedOutputViewModel)
          {
            OutputErrorViewModel.OutputState = None;
            PopulateObservations();
            PlotObservations();
          }
        }
      });
    }

    private void ObserveEvidenceObservationsChanges((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      PopulateObservations();
      PlotObservations();
    }

    private void ObserveSelectedOutputViewModel(object? _)
    {
      if (SelectedOutputViewModel.IsFound())
      {
        var outputViewModel = SelectedOutputViewModels[SelectedOutputViewModel];
        var outputState = _moduleState.OutputStates
          .Find(ps => ps.Name == outputViewModel.Name)
          .AssertSome();
        OutputErrorViewModel.OutputState = outputState;
        _moduleState.LikelihoodState.SelectedOutput = outputState.Name;
      }
      else
      {
        OutputErrorViewModel.OutputState = None;
        _moduleState.LikelihoodState.SelectedOutput = default;
      }

      PopulateObservations();
      PlotObservations();
    }

    private void ObserveOutputErrorViewModelOutputState(object _)
    {
      void Some(OutputState outputState)
      {
        var index = _moduleState.OutputStates.FindIndex(ps => ps.Name == outputState.Name);
        _moduleState.OutputStates = _moduleState.OutputStates.SetItem(index, outputState);

        var outputViewModel = AllOutputViewModels
          .Find(pvm => pvm.Name == outputState.Name)
          .AssertSome();

        RequireTrue(outputViewModel.IsSelected);
        RequireTrue(outputViewModel == SelectedOutputViewModels[SelectedOutputViewModel]);

        outputViewModel.ErrorModel = outputState.GetErrorModel().ToString(outputState.Name);
      }

      void None()
      {
        var outputViewModel = SelectedOutputViewModels[SelectedOutputViewModel];
        SelectedOutputViewModels.Remove(outputViewModel);
        outputViewModel.IsSelected = false;
        _moduleState.LikelihoodState.SelectedOutput = default;

        var index = _moduleState.OutputStates.FindIndex(ps => ps.Name == outputViewModel.Name);
        var outputState = _moduleState.OutputStates[index];
        RequireTrue(outputState.IsSelected);
        outputState = outputState.WithIsSelected(false);
        _moduleState.OutputStates = _moduleState.OutputStates.SetItem(index, outputState);
      }

      _outputErrorViewModel.OutputState.Match(Some, None);
    }

    private void PopulateObservations()
    {
      _observationsViewModelIsSelectedSubscriptions?.Dispose();
      _observationsViewModelIsSelectedSubscriptions = null;

      if (SelectedOutputViewModel.IsntFound())
      {
        ObservationsViewModels = default;
        return;
      }

      var outputViewModel = SelectedOutputViewModels[SelectedOutputViewModel];

      var observationsSet = _evidence.GetObservationSet(outputViewModel.Name);
      var observationsViewModels = observationsSet.Observations.Map(o =>
      {
        var evidenceSource = _evidence.EvidenceSources
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

    private void ObserveObservationsViewModelIsSelected(IObservedChange<IObservationsViewModel, bool> change)
    {
      var observationsViewModel = change.Sender as IObservationsViewModel;
      var outputViewModel = SelectedOutputViewModels[SelectedOutputViewModel];
      var observationsSet = _evidence.GetObservationSet(outputViewModel.Name);
      var observations = observationsSet.Observations
        .Find(o => o.ID == observationsViewModel.ID)
        .AssertSome();

      if (observationsViewModel.IsSelected)
      {
        _moduleState.SelectObservations(observations);
      }
      else
      {
        _moduleState.UnselectObservations(observations);
      }

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

      RequireTrue(SelectedOutputViewModel.IsFound());
      var outputName = SelectedOutputViewModels[SelectedOutputViewModel].Name;

      var verticalAxis = _observationsScatterPlot.GetAxis(AxisPosition.Left);
      if (!outputName.Equals(verticalAxis.Tag))
      {
        var output = _simulation.SimConfig.SimOutput;
        var dvElement = output.FindElement(outputName).AssertSome();
        verticalAxis.Title = dvElement.GetFQName();
        verticalAxis.Tag = outputName;

        _observationsScatterPlot.Series.Clear();
      }

      var observationsSet = _evidence.GetObservationSet(outputName);

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
      var horizontalAxis = _observationsScatterPlot.GetAxis(AxisPosition.Bottom);
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

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _observationsViewModelIsSelectedSubscriptions?.Dispose();
          _observationsViewModelIsSelectedSubscriptions = null;
          _subscriptions.Dispose();
          _outputErrorViewModel.Dispose();
        }

        _disposed = true;
      }
    }

    private const int PLOT_COLOR_COUNT = 8;

    private readonly IAppSettings _appSettings;
    private readonly Simulation _simulation;
    private readonly ISimEvidence _evidence;
    private readonly ModuleState _moduleState;
    private readonly PlotModel _observationsScatterPlot;
    private readonly ICommand _toggleSelectOutput;
    private readonly OutputErrorViewModel _outputErrorViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private IDisposable? _observationsViewModelIsSelectedSubscriptions;
    private bool _disposed = false;
  }
}
