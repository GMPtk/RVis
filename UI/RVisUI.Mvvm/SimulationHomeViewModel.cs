using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.ProcessHelper;
using static System.Environment;
using static System.IO.Path;

namespace RVisUI.Mvvm
{
  public sealed class SimulationHomeViewModel : ReactiveObject, ISimulationHomeViewModel
  {
    public SimulationHomeViewModel(IAppState appState, IAppService appService)
    {
      _appState = appState;
      _appService = appService;

      SharedStateViewModel = new SharedStateViewModel(appState, appService);

      BusyCancel = ReactiveCommand.Create(HandleBusyCancel);
      ChangeCommonConfiguration = ReactiveCommand.Create(HandleChangeCommonConfiguration);
      Export = ReactiveCommand.Create(
        HandleExport,
        appState.WhenAny(
          @as => @as.ActiveUIComponent,
          _ => appState.ActiveUIComponent.ViewModel is IExportedDataProvider
          ));
      Close = ReactiveCommand.Create(HandleClose);

      _appState.Simulation.Subscribe(
        appService.SafeInvoke<Option<Simulation>>(
          ObserveSimulation
          )
        );

      _appState
        .ObservableForProperty(s => s.UIComponents)
        .Subscribe(appService.SafeInvoke<object>(ObserveUIComponents));

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _appState
        .WhenAny(s => s.ActiveUIComponent, _ => default(object))
        .Subscribe(ObserveActiveUIComponent);

      this
        .ObservableForProperty(vm => vm.UIComponentIndex)
        .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveUIComponentIndex));

      MonitorTaskRunners();
    }

    public string? Name
    {
      get => _name;
      set => this.RaiseAndSetIfChanged(ref _name, value);
    }
    private string? _name;

    public ICommand ChangeCommonConfiguration { get; }

    public ICommand Export { get; }

    public ICommand Close { get; }

    private void ObserveTaskRunnerMessage(string message) => BusyMessages.Add(message);

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

    public ObservableCollection<string> BusyMessages { get; } = new ObservableCollection<string>();

    public bool EnableBusyCancel
    {
      get => _enableBusyCancel;
      set => this.RaiseAndSetIfChanged(ref _enableBusyCancel, value);
    }
    private bool _enableBusyCancel;

    public ICommand BusyCancel { get; }

    public ISharedStateViewModel SharedStateViewModel { get; }

    public int UIComponentIndex
    {
      get => _uiComponentIndex;
      set => this.RaiseAndSetIfChanged(ref _uiComponentIndex, value);
    }
    private int _uiComponentIndex;

    public string? ActiveUIComponentName
    {
      get => _activeUIComponentName;
      set => this.RaiseAndSetIfChanged(ref _activeUIComponentName, value);
    }
    private string? _activeUIComponentName;

    private void HandleExport()
    {
      var exportedDataProvider = RequireInstanceOf<IExportedDataProvider>(
        _appState.ActiveUIComponent.ViewModel
        );

      try
      {
        var rootExportDirectory = Combine(
          GetFolderPath(SpecialFolder.MyDocuments),
          "RVisData"
          );

        var dataExportConfiguration = exportedDataProvider.GetDataExportConfiguration(rootExportDirectory);

        var dataExportConfigurationViewModel = new DataExportConfigurationViewModel(
          _appService
          )
        {
          DataExportConfiguration = dataExportConfiguration
        };

        var didOK = _appService.ShowDialog(dataExportConfigurationViewModel, default);

        if (didOK)
        {
          dataExportConfiguration = dataExportConfigurationViewModel.DataExportConfiguration;

          exportedDataProvider.ExportData(dataExportConfiguration);

          if (dataExportConfiguration.OpenAfterExport)
          {
            OpenUrl(Combine(
              dataExportConfiguration.RootExportDirectory,
              dataExportConfiguration.ExportDirectoryName
              ));
          }
          else
          {
            _appState.Status = $"Export succeeded: {dataExportConfiguration.ExportDirectoryName}";
          }
        }
      }
      catch (Exception ex)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(SimulationHomeViewModel),
          nameof(DataExportConfiguration),
          ex.Message
          );
      }
    }

    private void HandleChangeCommonConfiguration()
    {
      var commonConfigurationViewModel = new CommonConfigurationViewModel(_appState);
      var didOK = _appService.ShowDialog(commonConfigurationViewModel, default);

      if (didOK)
      {

      }
    }

    private void HandleClose()
    {
      _taskRunners.Clear();
      _taskRunnerSubscriptions?.Dispose();
      _taskRunnerSubscriptions = null;
      IsBusy = false;
      _appState.Target = None;
    }

    private void HandleBusyCancel()
    {
      var cancelableTasks = _taskRunners
        .Where(kvp => kvp.Key.CanCancelTask)
        .Select(kvp => new { kvp.Value.AddedOn, kvp.Key })
        .OrderByDescending(a => a.AddedOn);

      if (!cancelableTasks.Any()) return;

      cancelableTasks.First().Key.HandleCancelTask();
    }

    private void ObserveSimulation(Option<Simulation> simulation)
    {
      simulation.Match(
        s =>
        {
          Name = s.SimConfig.Title;
        },
        () =>
        {
          Name = null;
        });
    }

    private void ObserveUIComponents(object _)
    {
      MonitorTaskRunners();

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        UIComponentIndex = _appState.UIComponents.FindIndex(uic => uic.ID == _appState.ActiveUIComponent.ID);
      }

      ActiveUIComponentName = _appState.ActiveUIComponent.DisplayName;
    }

    private void ObserveActiveUIComponent(object? _)
    {
      UIComponentIndex = _appState.UIComponents.FindIndex(uic => uic.ID == _appState.ActiveUIComponent.ID);
      ActiveUIComponentName = _appState.ActiveUIComponent.DisplayName;
    }

    private void ObserveUIComponentIndex(object _)
    {
      _appState.ActiveUIComponent =
        UIComponentIndex.IsFound() && _appState.UIComponents.Count > UIComponentIndex
        ? _appState.UIComponents[UIComponentIndex]
        : default;

      ActiveUIComponentName = _appState.ActiveUIComponent.DisplayName;
    }

    private void MonitorTaskRunners()
    {
      _taskRunnerSubscriptions?.Dispose();
      _taskRunnerSubscriptions = null;

      var taskRunners = new List<ITaskRunner>();

      var viewModels = _appState.UIComponents.Map(c => c.ViewModel);

      foreach (var viewModel in viewModels)
      {
        if (viewModel is ITaskRunner taskRunner)
        {
          taskRunners.Add(taskRunner);
        }

        if (viewModel is ITaskRunnerContainer taskRunnerContainer)
        {
          taskRunners.AddRange(taskRunnerContainer.GetTaskRunners());
        }
      }

      if (!taskRunners.Any()) return;

      var alreadyRunningTask = taskRunners.Where(tr => tr.IsRunningTask).ToList();
      alreadyRunningTask.ForEach(ObserveTaskRunner);

      var observeTaskRunner = _appService.SafeInvoke<IObservedChange<ITaskRunner, bool>>(
        oc => ObserveTaskRunner(oc.Sender)
        );

      var subscriptions = taskRunners.Select(
        tr => tr
          .ObservableForProperty(o => o.IsRunningTask)
          .Subscribe(observeTaskRunner)
        );

      _taskRunnerSubscriptions = new CompositeDisposable(subscriptions);
    }

    private void ObserveTaskRunner(ITaskRunner taskRunner)
    {
      if (0 == _taskRunners.Count) BusyMessages.Clear();

      var isRunningTask = taskRunner.IsRunningTask;

      if (!_taskRunners.ContainsKey(taskRunner))
      {
        _taskRunners.Add(taskRunner,
          (
            DateTime.UtcNow,
            0,
            taskRunner.TaskMessages.Subscribe(ObserveTaskRunnerMessage)
          ));
      }

      var (addedOn, nRunningTasks, messageSubscription) = _taskRunners[taskRunner];

      nRunningTasks += isRunningTask ? 1 : -1;

      if (nRunningTasks > 0)
      {
        _taskRunners[taskRunner] = (addedOn, nRunningTasks, messageSubscription);
      }
      else
      {
        _taskRunners.Remove(taskRunner);
        messageSubscription.Dispose();
      }

      IsBusy = _taskRunners.Aggregate(0, (c, kvp) => c + kvp.Value.NRunningTasks) > 0;

      BusyWith = _taskRunners.Count > 0 ?
        _taskRunners.OrderByDescending(kvp => kvp.Value.AddedOn).First().Key.TaskName :
        null;

      EnableBusyCancel = _taskRunners.Keys.Any(k => k.CanCancelTask);
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private CompositeDisposable? _taskRunnerSubscriptions;
    private readonly IDictionary<ITaskRunner, (DateTime AddedOn, int NRunningTasks, IDisposable MessageSubscription)> _taskRunners =
      new Dictionary<ITaskRunner, (DateTime AddedOn, int NRunningTasks, IDisposable MessageSubscription)>();
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
  }
}
