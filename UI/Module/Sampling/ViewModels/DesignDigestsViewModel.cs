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
using static RVis.Base.Check;
using static LanguageExt.Prelude;
using LanguageExt;

namespace Sampling
{
  internal sealed class DesignDigestsViewModel : IDesignDigestsViewModel, INotifyPropertyChanged, IDisposable
  {
    internal DesignDigestsViewModel(
      IAppService appService, 
      ModuleState moduleState, 
      SamplingDesigns samplingDesigns
      )
    {
      _moduleState = moduleState;
      _samplingDesigns = samplingDesigns;

      LoadSamplingDesign = ReactiveCommand.Create(
        HandleLoadSamplingDesign,
        this.WhenAny(
          vm => vm.SelectedDesignDigestViewModel, 
          _ => SelectedDesignDigestViewModel != default
          )
        );

      DeleteSamplingDesign = ReactiveCommand.Create(
        HandleDeleteSamplingDesign,
        this.WhenAny(
          vm => vm.SelectedDesignDigestViewModel, 
          _ => SelectedDesignDigestViewModel != default
          )
        );

      FollowKeyboardInDesignDigests = ReactiveCommand.Create<(Key Key, bool Control, bool Shift)>(
        HandleFollowKeyboardInDesignDigests
        );

      DesignDigestViewModels = new ObservableCollection<IDesignDigestViewModel>(
        samplingDesigns.DesignDigests.Map(
          dd => new DesignDigestViewModel(dd.CreatedOn, dd.Description)
        ));

      if (_moduleState.SamplingDesign != default)
      {
        TargetSamplingDesign = Some((_moduleState.SamplingDesign.CreatedOn, DateTime.Now));

        SelectedDesignDigestViewModel = DesignDigestViewModels
          .Find(vm => vm.CreatedOn == _moduleState.SamplingDesign.CreatedOn)
          .Match<IDesignDigestViewModel?>(vm => vm, () => default);
      }

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        moduleState
          .ObservableForProperty(ms => ms.SamplingDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSamplingDesign
            )
          ),

        samplingDesigns.SamplingDesignChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(DesignDigest, ObservableQualifier)>(
            ObserveSamplingDesignChange
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

    public ICommand LoadSamplingDesign { get; }

    public ICommand DeleteSamplingDesign { get; }

    public ICommand FollowKeyboardInDesignDigests { get; }

    public Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetSamplingDesign
    {
      get => _targetSamplingDesign;
      set => this.RaiseAndSetIfChanged(ref _targetSamplingDesign, value, PropertyChanged);
    }
    private Option<(DateTime CreatedOn, DateTime SelectedOn)> _targetSamplingDesign;

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

    private void HandleLoadSamplingDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        RequireNotNull(SelectedDesignDigestViewModel);

        TargetSamplingDesign = (SelectedDesignDigestViewModel.CreatedOn, DateTime.Now);
      }
    }

    private void HandleDeleteSamplingDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        RequireNotNull(SelectedDesignDigestViewModel);

        var createdOn = SelectedDesignDigestViewModel.CreatedOn;

        DesignDigestViewModels.Remove(SelectedDesignDigestViewModel);

        _samplingDesigns.Remove(createdOn);

        TargetSamplingDesign.IfSome(t =>
        {
          if (t.CreatedOn == createdOn)
          {
            TargetSamplingDesign = default;
          }
        });
      }
    }

    private void HandleFollowKeyboardInDesignDigests((Key Key, bool Control, bool Shift) state)
    {
      var (key, control, shift) = state;

      if (key == Key.Enter && !control && !shift) HandleLoadSamplingDesign();
    }

    private void ObserveModuleStateSamplingDesign(object _)
    {
      if (_moduleState.SamplingDesign != default)
      {
        _targetSamplingDesign = Some((_moduleState.SamplingDesign.CreatedOn, DateTime.Now));

        SelectedDesignDigestViewModel = DesignDigestViewModels
          .Find(vm => vm.CreatedOn == _moduleState.SamplingDesign.CreatedOn)
          .Match<IDesignDigestViewModel?>(vm => vm, () => default);
      }
      else
      {
        _targetSamplingDesign = None;

        SelectedDesignDigestViewModel = default;
      }
    }

    private void ObserveSamplingDesignChange(
      (DesignDigest DesignDigest, ObservableQualifier ObservableQualifier) change
      )
    {
      if (change.ObservableQualifier == ObservableQualifier.Add)
      {
        var designDigestViewModel = new DesignDigestViewModel(
          change.DesignDigest.CreatedOn, 
          change.DesignDigest.Description
          );
        DesignDigestViewModels.Insert(0, designDigestViewModel);
      }
      else if (change.ObservableQualifier == ObservableQualifier.Change)
      {
        var index = DesignDigestViewModels.FindIndex(
          vm => vm.CreatedOn == change.DesignDigest.CreatedOn
          );
        RequireTrue(index.IsFound());
        var designDigestViewModel = new DesignDigestViewModel(
          change.DesignDigest.CreatedOn, 
          change.DesignDigest.Description
          );
        DesignDigestViewModels[index] = designDigestViewModel;
      }
      else if (change.ObservableQualifier == ObservableQualifier.Remove)
      {
        var index = DesignDigestViewModels.FindIndex(
          vm => vm.CreatedOn == change.DesignDigest.CreatedOn
          );
        RequireTrue(index.IsFound());
        var designDigest = DesignDigestViewModels[index];
        TargetSamplingDesign.IfSome(t =>
        {
          if (t.CreatedOn == designDigest.CreatedOn)
          {
            TargetSamplingDesign = default;
          }
        });
        DesignDigestViewModels.RemoveAt(index);
      }
    }

    private readonly ModuleState _moduleState;
    private readonly SamplingDesigns _samplingDesigns;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
