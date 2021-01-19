using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace Plot
{
  internal sealed class TraceViewModel : ViewModelBase, ITraceViewModel
  {
    public TraceViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      Arr<ParameterViewModel> parameterViewModels,
      ModuleState moduleState,
      OutputGroupStore outputGroupStore
      )
    {
      RequireNotNull(SynchronizationContext.Current);

      _appState = appState;
      _appService = appService;
      _parameterViewModels = parameterViewModels;
      _moduleState = moduleState;
      _outputGroupStore = outputGroupStore;

      _simulation = appState.Target.AssertSome("No simulation");

      TraceDataPlotViewModels = Range(1, ModuleState.N_TRACES)
          .Map(i =>
            new TraceDataPlotViewModel(
              appState,
              appService,
              appSettings,
              moduleState.TraceDataPlotStates[i - 1]
              )
            )
         .ToArr<ITraceDataPlotViewModel>();

      var nTracesVisible = _moduleState.TraceDataPlotStates.Count(s => s.IsVisible);
      ChartGridLayout = nTracesVisible - 1;

      WorkingSet = new ObservableCollection<IParameterViewModel>(
        parameterViewModels.Filter(vm => vm.IsSelected)
        );

      UndoWorkingChange = ReactiveCommand.Create(
        HandleUndoWorkingChange,
        this.WhenAny(vm => vm.SessionEdits, _ => SessionEdits.Count > 1)
        );

      PlotWorkingChanges = ReactiveCommand.Create(
        HandlePlotWorkingChanges,
        this.WhenAny(vm => vm.HasPendingWorkingChanges, _ => HasPendingWorkingChanges)
        );

      _isWorkingSetPanelOpen = _moduleState.TraceState.IsWorkingSetPanelOpen
        ? 0
        : -1;

      var (ms, _) = _appState.SimData
        .GetExecutionInterval(_simulation)
        .IfNone((304, default));

      var dueTime = TimeSpan.FromMilliseconds(20 + ms * 1.25);

      var parameterEditStatesObservable = Observable
        .Merge(
          _moduleState.ParameterEditStates.Map(
            pes => pes.ObservableForProperty(inpc => inpc.Value)
            )
          );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      var subscriptions = new[]
      {
        appState.SimData.OutputRequests
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(ObserveOutputRequest),

        this
          .ObservableForProperty(vm => vm.IsSelected)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveIsSelected
              )
            ),

        this
          .ObservableForProperty(vm => vm.IsWorkingSetPanelOpen)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveIsWorkingSetPanelOpen
              )
            ),

        Observable
          .Merge(
            moduleState.TraceDataPlotStates.Map(
              s => s.ObservableForProperty(t => t.IsVisible)
              )
            )
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<IObservedChange<TraceDataPlotState, bool>>(
              ObserveTraceDataPlotIsVisible
              )
            ),

        parameterEditStatesObservable
          .Throttle(dueTime)
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveOnlineParameterEditStateValue
              )
            ),

        parameterEditStatesObservable
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveOfflineParameterEditStateValue
              )
            ),

        Observable
          .Merge(
            _moduleState.ParameterEditStates.Map(
              pes => pes.ObservableForProperty(inpc => inpc.IsSelected)
              )
            )
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<IObservedChange<ParameterEditState, bool>>(
              ObserveParameterEditStateIsSelected
              )
            )

      };

      _subscriptions = new CompositeDisposable(subscriptions);

      ApplyParameterEditState();
    }

    public Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> DataSet
    {
      get => _dataSet;
      set => this.RaiseAndSetIfChanged(ref _dataSet, value);
    }
    private Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> _dataSet;

    public Arr<ITraceDataPlotViewModel> TraceDataPlotViewModels
    {
      get => _traceDataPlotViewModels;
      set => this.RaiseAndSetIfChanged(ref _traceDataPlotViewModels, value);
    }
    private Arr<ITraceDataPlotViewModel> _traceDataPlotViewModels;

    public int ChartGridLayout
    {
      get => _chartGridLayout;
      set
      {
        UpdateTraceDataPlotState(_moduleState.TraceDataPlotStates, value);
        this.RaiseAndSetIfChanged(ref _chartGridLayout, value);
      }
    }
    private int _chartGridLayout;

    public int IsWorkingSetPanelOpen
    {
      get => _isWorkingSetPanelOpen;
      set => this.RaiseAndSetIfChanged(ref _isWorkingSetPanelOpen, value);
    }
    private int _isWorkingSetPanelOpen = -1;

    public ObservableCollection<IParameterViewModel> WorkingSet { get; }

    public Stck<SimInput> SessionEdits
    {
      get => _sessionEdits;
      set => this.RaiseAndSetIfChanged(ref _sessionEdits, value);
    }
    private Stck<SimInput> _sessionEdits;

    public ICommand UndoWorkingChange { get; }

    public ICommand PlotWorkingChanges { get; }

    public bool HasPendingWorkingChanges
    {
      get => _hasPendingWorkingChanges;
      set => this.RaiseAndSetIfChanged(ref _hasPendingWorkingChanges, value);
    }
    private bool _hasPendingWorkingChanges;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
    private bool _isSelected;

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        _subscriptions.Dispose();

        TraceDataPlotViewModels
          .OfType<IDisposable>()
          .ToArr()
          .Iter(d => d.Dispose());
      }

      base.Dispose(disposing);
    }

    private void ApplyParameterEditState()
    {
      var parameters = _simulation.SimConfig.SimInput.SimParameters;

      var applied = _moduleState.ParameterEditStates
        .Filter(pes => pes.IsSelected)
        .Map(pes =>
        {
          var parameter = parameters.GetParameter(pes.Name.AssertNotNull());
          return parameter.Value == pes.Value ? parameter : parameter.With(pes.Value);
        });

      var input = _simulation.SimConfig.SimInput.With(applied);

      if (SessionEdits.IsEmpty || input.Hash != SessionEdits.Peek().Hash)
      {
        SessionEdits = SessionEdits.Push(input);
        RequestOutput(input, DataRequestType.WorkingSet);
      }

      HasPendingWorkingChanges = false;
    }

    private void RequestOutput(SimInput input, DataRequestType dataRequestType)
    {
      TaskName = "Acquiring Output";
      IsRunningTask = true;
      _appState.SimData.RequestOutput(_simulation, input, this, dataRequestType, true);
    }

    private static void UpdateTraceDataPlotState(Arr<TraceDataPlotState> traceDataPlotStates, int chartGridLayout)
    {
      var nTracesCurrentlyVisible = traceDataPlotStates.Count(s => s.IsVisible);
      var nTracesRequiredVisible = chartGridLayout + 1;

      while (nTracesRequiredVisible > nTracesCurrentlyVisible)
      {
        var firstNotUsed = traceDataPlotStates.First(s => !s.IsVisible);
        firstNotUsed.IsVisible = true;
        ++nTracesCurrentlyVisible;
      }

      while (nTracesCurrentlyVisible > nTracesRequiredVisible)
      {
        var lastUsed = traceDataPlotStates.Last(s => s.IsVisible);
        lastUsed.IsVisible = false;
        --nTracesCurrentlyVisible;
      }
    }

    private void HandleUndoWorkingChange()
    {
      RequireTrue(SessionEdits.Count > 1);

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        SessionEdits = SessionEdits.Pop();
        var input = SessionEdits.Peek();

        input.SimParameters.Iter(p =>
        {
          var parameterEditState = _moduleState.ParameterEditStates
            .Find(pes => pes.Name == p.Name)
            .AssertSome();

          parameterEditState.Value = p.Value;
        });

        RequestOutput(input, DataRequestType.WorkingSet);
        HasPendingWorkingChanges = false;
      }
    }

    private void HandlePlotWorkingChanges()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        ApplyParameterEditState();
      }
    }

    private void ObserveOutputRequest(SimDataItem<OutputRequest> observed)
    {
      if (observed.IsSimDataEvent(out SimDataEvent _))
      {
        IsRunningTask = false;
        return;
      }

      if (observed.RequestToken is not DataRequestType dataRequestType) return;
      if (dataRequestType != DataRequestType.WorkingSet) return;

      void WithSerieInputs(Arr<SimInput> serieInputs)
      {
        if (serieInputs.Count == 1)
        {
          var serieInput = serieInputs[0];
          var serie = _appState.SimData.GetOutput(serieInput, _simulation).AssertSome();
          SetDataSet(observed.Item.SeriesInput, Array((serieInput, serie)));
        }
        else
        {
          OutputGroup? outputGroup;

          if (_outputGroupStore.IsActiveGroupSeries(serieInputs))
          {
            outputGroup = _outputGroupStore.ActiveOutputGroup.AssertSome();
          }
          else
          {
            outputGroup = OutputGroup.Create("Working set series", observed);
            _outputGroupStore.AddAndActivate(outputGroup);
          }

          var serieInfo = serieInputs.Map(i =>
          {
            var serie = _appState.SimData.GetOutput(i, _simulation).AssertSome();
            return (i, serie);
          });

          SetDataSet(outputGroup.Name, serieInfo.ToArr());
        }
      }

      void WithException(Exception ex)
      {
        Logger.Log.Error(ex, nameof(ObserveOutputRequest));

        _appService.Notify(
          nameof(TraceViewModel),
          nameof(ObserveOutputRequest),
          ex
          );

        if (SessionEdits.Peek().Hash == observed.Item.SeriesInput.Hash)
        {
          using (_reactiveSafeInvoke.SuspendedReactivity)
          {
            SessionEdits = SessionEdits.Pop();
          }
        }
      }

      observed.Item.SerieInputs.Match(WithSerieInputs, WithException);

      IsRunningTask = false;
    }

    private void ObserveIsSelected(object _)
    {
      if (_isSelected && HasPendingWorkingChanges)
      {
        ApplyParameterEditState();
      }
    }

    private void ObserveIsWorkingSetPanelOpen(object _)
    {
      _moduleState.TraceState.IsWorkingSetPanelOpen = IsWorkingSetPanelOpen != -1;
    }

    private void ObserveTraceDataPlotIsVisible(IObservedChange<TraceDataPlotState, bool> _)
    {
      var nTracesVisible = _moduleState.TraceDataPlotStates.Count(s => s.IsVisible);
      ChartGridLayout = nTracesVisible - 1;
    }

    private void ObserveOnlineParameterEditStateValue(object _)
    {
      if (!IsSelected) return;

      HasPendingWorkingChanges = true;

      var allNumeric = WorkingSet.ForAll(vm => vm.NValue.HasValue);

      if (!allNumeric) return;

      ApplyParameterEditState();
    }

    private void ObserveOfflineParameterEditStateValue(object _)
    {
      if (IsSelected) return;

      HasPendingWorkingChanges = true;
    }

    private void ObserveParameterEditStateIsSelected(IObservedChange<ParameterEditState, bool> change)
    {
      var parameterEditState = change.Sender;

      var parameterViewModel = _parameterViewModels
        .Find(vm => vm.Name == parameterEditState.Name)
        .AssertSome();

      if (parameterEditState.IsSelected)
      {
        WorkingSet.Insert(0, parameterViewModel);
      }
      else
      {
        WorkingSet.Remove(parameterViewModel);
      }

      HasPendingWorkingChanges = true;
      if (IsSelected) ApplyParameterEditState();
    }

    private void SetDataSet(SimInput seriesInput, Arr<(SimInput SerieInput, NumDataTable Serie)> serieInfo)
    {
      var seriesParameters = _simulation.SimConfig.SimInput.GetEdits(seriesInput);
      var seriesName = seriesParameters.IsEmpty ? "Default" : seriesParameters.ToAssignments("G4");
      SetDataSet(seriesName, serieInfo);
    }

    private void SetDataSet(string seriesName, Arr<(SimInput SerieInput, NumDataTable Serie)> serieInfo)
    {
      var series = serieInfo.Map(si =>
      {
        var serieParameters = _simulation.SimConfig.SimInput.GetEdits(si.SerieInput);
        var serieName = serieParameters.IsEmpty ? "Default" : serieParameters.ToAssignments("G4");
        var serie = si.Serie;
        return (serieName, serie);
      });

      DataSet = Array((seriesName, series));
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly Arr<ParameterViewModel> _parameterViewModels;
    private readonly ModuleState _moduleState;
    private readonly OutputGroupStore _outputGroupStore;
    private readonly Simulation _simulation;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
  }
}


//var config = _simulation.SimConfig;

//_appState.SimData.GetOutput(config).Match(
//  ndt => DataSet = Array((ndt.Name, ndt)),
//  () =>
//  {
//    TaskName = "Plot - Acquiring Output";
//    IsRunningTask = true;
//    _appState.SimData.RequestOutput(config, this, "xxx", true);
//  });

//var sc = SynchronizationContext.Current;
//Task.Run(async () =>
//{
//  await Task.Delay(1000);
//  sc.Send((_) => { TaskName = "Plot - Acquiring Output"; IsRunningTask = true; }, null);

//  for (var i = 0; i < 10; ++i)
//  {
//    sc.Send((j) => { RaiseTaskMessageEvent($"{j:00000}"); }, i);
//    await Task.Delay(500);
//  }

//  sc.Send(_ => _appState.SimData.RequestOutput(_simulation.SimConfig, this, "xxx", true), null);
//});

//var A = _simulation.SimConfig.SimInput.SimParameters.Find(p => p.Name == "A").AssertSome();
//var tweakedA = A.With("seq(45, 55, by=1)");
//var workingSet = Array(tweakedA);
//var seriesInput = _simulation.SimConfig.SimInput.With(workingSet);
////_plotState.ParameterEditStates = workingSet;

//RequestOutput(Array(seriesInput), DataRequestType.ForEdits);
