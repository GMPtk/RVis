﻿using LanguageExt;
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

namespace Sensitivity
{
  internal sealed class DesignDigestsViewModel : IDesignDigestsViewModel, INotifyPropertyChanged, IDisposable
  {
    internal DesignDigestsViewModel(IAppService appService, ModuleState moduleState, SensitivityDesigns sensitivityDesigns)
    {
      _moduleState = moduleState;
      _sensitivityDesigns = sensitivityDesigns;

      LoadSensitivityDesign = ReactiveCommand.Create(
        HandleLoadSensitivityDesign,
        this.WhenAny(vm => vm.SelectedDesignDigestViewModel, _ => SelectedDesignDigestViewModel != default)
        );

      DeleteSensitivityDesign = ReactiveCommand.Create(
        HandleDeleteSensitivityDesign,
        this.WhenAny(vm => vm.SelectedDesignDigestViewModel, _ => SelectedDesignDigestViewModel != default)
        );

      FollowKeyboardInDesignDigests = ReactiveCommand.Create<(Key Key, bool Control, bool Shift)>(
        HandleFollowKeyboardInDesignDigests
        );

      DesignDigestViewModels = new ObservableCollection<IDesignDigestViewModel>(
        sensitivityDesigns.DesignDigests.Map(dd => new DesignDigestViewModel(dd.CreatedOn, dd.Description)
        ));

      _targetSensitivityDesignCreatedOn = _moduleState.SensitivityDesign == default
        ? None
        : Some((_moduleState.SensitivityDesign.CreatedOn, DateTime.Now));

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        moduleState
          .ObservableForProperty(ms => ms.SensitivityDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSensitivityDesign
            )
          ),

        sensitivityDesigns.SensitivityDesignChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(DesignDigest, ObservableQualifier)>(
            ObserveSensitivityDesignChange
            )
          )

        );
    }

    public ObservableCollection<IDesignDigestViewModel> DesignDigestViewModels { get; }

    public IDesignDigestViewModel SelectedDesignDigestViewModel
    {
      get => _selectedDesignDigestViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedDesignDigestViewModel, value, PropertyChanged);
    }
    private IDesignDigestViewModel _selectedDesignDigestViewModel;

    public ICommand LoadSensitivityDesign { get; }

    public ICommand DeleteSensitivityDesign { get; }

    public ICommand FollowKeyboardInDesignDigests { get; }

    public Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetSensitivityDesign
    {
      get => _targetSensitivityDesignCreatedOn;
      set => this.RaiseAndSetIfChanged(ref _targetSensitivityDesignCreatedOn, value, PropertyChanged);
    }
    private Option<(DateTime CreatedOn, DateTime SelectedOn)> _targetSensitivityDesignCreatedOn;

    public event PropertyChangedEventHandler PropertyChanged;

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

    private void HandleLoadSensitivityDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        TargetSensitivityDesign = (SelectedDesignDigestViewModel.CreatedOn, DateTime.Now);
      }
    }

    private void HandleDeleteSensitivityDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var createdOn = SelectedDesignDigestViewModel.CreatedOn;

        DesignDigestViewModels.Remove(SelectedDesignDigestViewModel);

        _sensitivityDesigns.Remove(createdOn);

        TargetSensitivityDesign.IfSome(t =>
        {
          if (t.CreatedOn == createdOn)
          {
            TargetSensitivityDesign = default;
          }
        });
      }
    }

    private void HandleFollowKeyboardInDesignDigests((Key Key, bool Control, bool Shift) state)
    {
      var (key, control, shift) = state;

      if (key == Key.Enter && !control && !shift) HandleLoadSensitivityDesign();
    }

    private void ObserveModuleStateSensitivityDesign(object _)
    {
      _targetSensitivityDesignCreatedOn = _moduleState.SensitivityDesign == default
        ? None
        : Some((_moduleState.SensitivityDesign.CreatedOn, DateTime.Now));
    }

    private void ObserveSensitivityDesignChange((DesignDigest DesignDigest, ObservableQualifier ObservableQualifier) change)
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
        TargetSensitivityDesign.IfSome(t =>
        {
          if (t.CreatedOn == designDigest.CreatedOn)
          {
            TargetSensitivityDesign = default;
          }
        });
        DesignDigestViewModels.RemoveAt(index);
      }
    }

    private readonly ModuleState _moduleState;
    private readonly SensitivityDesigns _sensitivityDesigns;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}