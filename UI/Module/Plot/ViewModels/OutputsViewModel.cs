using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using static RVis.Base.Check;

namespace Plot
{
  internal sealed class OutputsViewModel : IOutputsViewModel, INotifyPropertyChanged, IDisposable
  {
    public OutputsViewModel(IAppState appState, OutputGroupStore outputGroupStore)
    {
      RequireNotNull(SynchronizationContext.Current);

      _simulation = appState.Target.AssertSome();
      _simData = appState.SimData;
      _outputGroupStore = outputGroupStore;

      LogEntryViewModels = new ObservableCollection<ILogEntryViewModel>();

      LoadLogEntry = ReactiveCommand.Create(
        HandleLoadLogEntry, 
        this.WhenAny(
          vm => vm.SelectedLogEntryViewModels, 
          _ => SelectedLogEntryViewModels?.Length == 1
          )
        );

      CreateOutputGroup = ReactiveCommand.Create(
        HandleCreateOutputGroup,
        this.WhenAny(
          vm => vm.SelectedLogEntryViewModels,
          _ => SelectedLogEntryViewModels?.Length > 1
          )
        );

      FollowKeyboardInLogEntries = ReactiveCommand.Create<(Key Key, bool Control, bool Shift)>(
        HandleFollowKeyboardInLogEntries
        );

      OutputGroupViewModels = new ObservableCollection<IOutputGroupViewModel>(
        outputGroupStore.OutputGroups.Map(og => new OutputGroupViewModel(og))
        );

      LoadOutputGroup = ReactiveCommand.Create(
        HandleLoadOutputGroup,
        this.WhenAny(vm => vm.SelectedOutputGroupViewModel, _ => SelectedOutputGroupViewModel != null)
        );

      FollowKeyboardInOutputGroups = ReactiveCommand.Create<(Key Key, bool Control, bool Shift)>(
        HandleFollowKeyboardInOutputGroups
        );

      _subscriptions = new CompositeDisposable(

        appState.SimDataSessionLog.LogEntries
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(ObserveLogEntry),

        outputGroupStore.ActivatedOutputGroups
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(ObserveActivateOutputGroup)

      );
    }

    public ObservableCollection<ILogEntryViewModel> LogEntryViewModels { get; }

    public ILogEntryViewModel[]? SelectedLogEntryViewModels
    {
      get => _selectedLogEntryViewModels;
      set => this.RaiseAndSetIfChanged(ref _selectedLogEntryViewModels, value, PropertyChanged);
    }
    public ILogEntryViewModel[]? _selectedLogEntryViewModels;

    public ICommand LoadLogEntry { get; }

    public ICommand CreateOutputGroup { get; }

    public ICommand FollowKeyboardInLogEntries { get; }

    public ObservableCollection<IOutputGroupViewModel> OutputGroupViewModels { get; }

    public IOutputGroupViewModel? SelectedOutputGroupViewModel
    {
      get => _selectedOutputGroupViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputGroupViewModel, value, PropertyChanged);
    }
    public IOutputGroupViewModel? _selectedOutputGroupViewModel;

    public ICommand LoadOutputGroup { get; }

    public ICommand FollowKeyboardInOutputGroups { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void HandleLoadLogEntry() => LoadSelectedLogEntries();

    private void HandleCreateOutputGroup() => LoadSelectedLogEntries();

    private void HandleFollowKeyboardInLogEntries((Key Key, bool Control, bool Shift) state)
    {
      var (key, control, shift) = state;

      if (key == Key.Enter && !control && !shift) LoadSelectedLogEntries();
    }

    private void LoadSelectedLogEntries()
    {
      var selectedLogEntries = SelectedLogEntryViewModels.Map(vm => vm.LogEntry).ToArr();

      if (selectedLogEntries.Count > 1)
      {
        var defaultInput = _simulation.SimConfig.SimInput;
        var outputGroup = OutputGroup.Create("Session log entries", selectedLogEntries, defaultInput);
        _outputGroupStore.AddAndActivate(outputGroup);
      }
      else
      {
        var outputInfo = _simData.GetOutputInfo(selectedLogEntries.Head().SerieInputHash, _simulation);
        var serieInput = outputInfo.AssertSome().SerieInput;
        _simData.RequestOutput(_simulation, serieInput, this, DataRequestType.LogEntry, false);
      }
    }

    private void ObserveLogEntry(SimDataLogEntry logEntry) =>
      LogEntryViewModels.Insert(
        0,
        new LogEntryViewModel(logEntry, _simulation.SimConfig.SimInput)
        );

    private void HandleLoadOutputGroup() => LoadSelectedOutputGroup();

    private void HandleFollowKeyboardInOutputGroups((Key Key, bool Control, bool Shift) state)
    {
      var (key, control, shift) = state;

      if (key == Key.Enter && !control && !shift) LoadSelectedOutputGroup();
    }

    private void LoadSelectedOutputGroup()
    {
      RequireNotNull(SelectedOutputGroupViewModel);

      var selectedOutputGroup = SelectedOutputGroupViewModel.OutputGroup;
      _outputGroupStore.Activate(selectedOutputGroup);
    }

    private void ObserveActivateOutputGroup((OutputGroup OutputGroup, bool Added) activeOutputGroup)
    {
      var (outputGroup, added) = activeOutputGroup;

      if (added)
      {
        var outputGroupViewModel = new OutputGroupViewModel(outputGroup);
        OutputGroupViewModels.Insert(0, outputGroupViewModel);
      }
      else
      {
        var indexExisting = OutputGroupViewModels.FindIndex(vm => vm.OutputGroup == outputGroup);
        RequireTrue(indexExisting.IsFound());
        if (indexExisting > 0)
        {
          var outputGroupViewModel = OutputGroupViewModels[indexExisting];
          OutputGroupViewModels.RemoveAt(indexExisting);
          OutputGroupViewModels.Insert(0, outputGroupViewModel);
        }
      }
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

    private readonly Simulation _simulation;
    private readonly ISimData _simData;
    private readonly OutputGroupStore _outputGroupStore;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
