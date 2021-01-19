using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace Estimation
{
  internal sealed class DesignDigestsViewModel : IDesignDigestsViewModel, INotifyPropertyChanged, IDisposable
  {
    internal DesignDigestsViewModel(IAppService appService, ModuleState moduleState, EstimationDesigns estimationDesigns)
    {
      _moduleState = moduleState;
      _estimationDesigns = estimationDesigns;

      LoadEstimationDesign = ReactiveCommand.Create(
        HandleLoadEstimationDesign,
        this.WhenAny(vm => vm.SelectedDesignDigestViewModel, _ => SelectedDesignDigestViewModel != default)
        );

      DeleteEstimationDesign = ReactiveCommand.Create(
        HandleDeleteEstimationDesign,
        this.WhenAny(vm => vm.SelectedDesignDigestViewModel, _ => SelectedDesignDigestViewModel != default)
        );

      FollowKeyboardInDesignDigests = ReactiveCommand.Create<(Key Key, bool Control, bool Shift)>(
        HandleFollowKeyboardInDesignDigests
        );

      DesignDigestViewModels = new ObservableCollection<IDesignDigestViewModel>(
        estimationDesigns.DesignDigests.Map(dd => new DesignDigestViewModel(dd.CreatedOn, dd.Description)
        ));

      if (_moduleState.EstimationDesign != default)
      {
        TargetEstimationDesign = Some((_moduleState.EstimationDesign.CreatedOn, DateTime.Now));

        SelectedDesignDigestViewModel = DesignDigestViewModels
          .Find(vm => vm.CreatedOn == _moduleState.EstimationDesign.CreatedOn)
          .Match<IDesignDigestViewModel?>(vm => vm, () => default);
      }

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        moduleState
          .ObservableForProperty(ms => ms.EstimationDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateEstimationDesign
            )
          ),

        estimationDesigns.EstimationDesignChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(DesignDigest, ObservableQualifier)>(
            ObserveEstimationDesignChange
            )
          )

        );
    }

    public ObservableCollection<IDesignDigestViewModel> DesignDigestViewModels { get; }

    public IDesignDigestViewModel? SelectedDesignDigestViewModel
    {
      get => _selectedDesignDigestViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedDesignDigestViewModel, value, PropertyChanged);
    }
    private IDesignDigestViewModel? _selectedDesignDigestViewModel;

    public ICommand LoadEstimationDesign { get; }

    public ICommand DeleteEstimationDesign { get; }

    public ICommand FollowKeyboardInDesignDigests { get; }

    public Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetEstimationDesign
    {
      get => _targetEstimationDesign;
      set => this.RaiseAndSetIfChanged(ref _targetEstimationDesign, value, PropertyChanged);
    }
    private Option<(DateTime CreatedOn, DateTime SelectedOn)> _targetEstimationDesign;

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

    private void HandleLoadEstimationDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        RequireNotNull(SelectedDesignDigestViewModel);

        TargetEstimationDesign = (SelectedDesignDigestViewModel.CreatedOn, DateTime.Now);
      }
    }

    private void HandleDeleteEstimationDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        RequireNotNull(SelectedDesignDigestViewModel);

        var createdOn = SelectedDesignDigestViewModel.CreatedOn;

        DesignDigestViewModels.Remove(SelectedDesignDigestViewModel);

        _estimationDesigns.Remove(createdOn);

        TargetEstimationDesign.IfSome(t =>
        {
          if (t.CreatedOn == createdOn)
          {
            TargetEstimationDesign = default;
          }
        });
      }
    }

    private void HandleFollowKeyboardInDesignDigests((Key Key, bool Control, bool Shift) state)
    {
      var (key, control, shift) = state;

      if (key == Key.Enter && !control && !shift) HandleLoadEstimationDesign();
    }

    private void ObserveModuleStateEstimationDesign(object _)
    {
      if (_moduleState.EstimationDesign != default)
      {
        _targetEstimationDesign = Some((_moduleState.EstimationDesign.CreatedOn, DateTime.Now));

        SelectedDesignDigestViewModel = DesignDigestViewModels
          .Find(vm => vm.CreatedOn == _moduleState.EstimationDesign.CreatedOn)
          .Match<IDesignDigestViewModel?>(vm => vm, () => default);
      }
      else
      {
        _targetEstimationDesign = None;

        SelectedDesignDigestViewModel = default;
      }
    }

    private void ObserveEstimationDesignChange((DesignDigest DesignDigest, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier == ObservableQualifier.Add)
      {
        var designDigestViewModel = new DesignDigestViewModel(change.DesignDigest.CreatedOn, change.DesignDigest.Description);
        DesignDigestViewModels.Insert(0, designDigestViewModel);
      }
      else if (change.ObservableQualifier == ObservableQualifier.Change)
      {
        var index = DesignDigestViewModels.FindIndex(vm => vm.CreatedOn == change.DesignDigest.CreatedOn);
        RequireTrue(index.IsFound());
        var designDigestViewModel = new DesignDigestViewModel(change.DesignDigest.CreatedOn, change.DesignDigest.Description);
        DesignDigestViewModels[index] = designDigestViewModel;
      }
      else if (change.ObservableQualifier == ObservableQualifier.Remove)
      {
        var index = DesignDigestViewModels.FindIndex(vm => vm.CreatedOn == change.DesignDigest.CreatedOn);
        RequireTrue(index.IsFound());
        var designDigest = DesignDigestViewModels[index];
        TargetEstimationDesign.IfSome(t =>
        {
          if (t.CreatedOn == designDigest.CreatedOn)
          {
            TargetEstimationDesign = default;
          }
        });
        DesignDigestViewModels.RemoveAt(index);
      }
    }

    private readonly ModuleState _moduleState;
    private readonly EstimationDesigns _estimationDesigns;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
