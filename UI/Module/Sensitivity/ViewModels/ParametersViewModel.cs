using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
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
using static Sensitivity.DesignParameter;

namespace Sensitivity
{
  internal sealed class ParametersViewModel : IParametersViewModel, INotifyPropertyChanged, IDisposable
  {
    internal ParametersViewModel(
      IAppState appState, 
      IAppService appService, 
      IAppSettings appSettings, 
      ModuleState moduleState
      )
    {
      _simulation = appState.Target.AssertSome("No simulation");
      _moduleState = moduleState;

      _toggleSelectParameter = ReactiveCommand.Create<IParameterViewModel>(
        HandleToggleSelectParameter
        );

      var parameters = _simulation.SimConfig.SimInput.SimParameters;

      AllParameterViewModels = parameters
        .Map(p => new ParameterViewModel(p.Name, _toggleSelectParameter))
        .OrderBy(vm => vm.SortKey)
        .ToArr<IParameterViewModel>();

      moduleState.ParameterStates.Iter(ps =>
      {
        var parameterViewModel = AllParameterViewModels
        .Find(vm => vm.Name == ps.Name)
        .AssertSome($"Unknown parameter in module state: {ps.Name}");

        parameterViewModel.IsSelected = ps.IsSelected;
        parameterViewModel.Distribution = ps.GetDistribution().ToString(ps.Name);
      });

      SelectedParameterViewModels = new ObservableCollection<IParameterViewModel>(
        AllParameterViewModels.Where(vm => vm.IsSelected)
        );

      SelectedParameterViewModel = SelectedParameterViewModels.FindIndex(
        vm => vm.Name == moduleState.ParametersState.SelectedParameter
        );

      _parameterDistributionViewModel = new ParameterDistributionViewModel(
        appState,
        appService,
        appSettings,
        SupportedDistTypes,
        allowTruncation: true
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        this
          .WhenAny(vm => vm.SelectedParameterViewModel, _ => default(object))
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object?>(
              ObserveSelectedParameterViewModel
              )
            ),

        _parameterDistributionViewModel
          .ObservableForProperty(vm => vm.ParameterState)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveParameterDistributionViewModelParameterState
              )
            ),

        moduleState.ParameterStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<ParameterState>, ObservableQualifier)>(
            ObserveModuleStateParameterStateChange
            )
          )

      );
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value, PropertyChanged);
    }
    private bool _isVisible;

    public Arr<IParameterViewModel> AllParameterViewModels { get; }

    public ObservableCollection<IParameterViewModel> SelectedParameterViewModels { get; }

    public int SelectedParameterViewModel
    {
      get => _selectedParameterViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedParameterViewModel, value, PropertyChanged);
    }
    private int _selectedParameterViewModel;

    public IParameterDistributionViewModel ParameterDistributionViewModel => _parameterDistributionViewModel;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void HandleToggleSelectParameter(IParameterViewModel parameterViewModel)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var index = _moduleState.ParameterStates.FindIndex(
          ps => ps.Name == parameterViewModel.Name
          );
        var isSelected = SelectedParameterViewModels.Contains(parameterViewModel);
        ParameterState parameterState;

        if (index.IsFound())
        {
          parameterState = _moduleState.ParameterStates[index];
          parameterState = parameterState.WithIsSelected(!isSelected);
          _moduleState.ParameterStates = _moduleState.ParameterStates.SetItem(
            index, 
            parameterState
            );
        }
        else
        {
          var parameters = _simulation.SimConfig.SimInput.SimParameters;
          var parameter = parameters.GetParameter(parameterViewModel.Name);
          parameterState = ParameterState.Create(parameter.Name, parameter.Scalar);
          _moduleState.ParameterStates += parameterState;
        }

        if (isSelected)
        {
          SelectedParameterViewModels.Remove(parameterViewModel);
          if (SelectedParameterViewModel.IsntFound())
          {
            _parameterDistributionViewModel.ParameterState = None;
            _moduleState.ParametersState.SelectedParameter = default;
          }
        }
        else
        {
          SelectedParameterViewModels.InsertInOrdered(parameterViewModel, pvm => pvm.SortKey);
          parameterViewModel.Distribution = parameterState
            .GetDistribution()
            .ToString(parameterViewModel.Name);
        }

        parameterViewModel.IsSelected = !isSelected;
      }
    }

    private void ObserveSelectedParameterViewModel(object? _)
    {
      UpdateSelectedParameter();
    }

    private void ObserveParameterDistributionViewModelParameterState(object _)
    {
      void Some(ParameterState parameterState)
      {
        var index = _moduleState.ParameterStates.FindIndex(
          ps => ps.Name == parameterState.Name
          );
        _moduleState.ParameterStates = _moduleState.ParameterStates.SetItem(
          index, 
          parameterState
          );

        var parameterViewModel = AllParameterViewModels
          .Find(pvm => pvm.Name == parameterState.Name)
          .AssertSome();

        RequireTrue(parameterViewModel.IsSelected);
        RequireTrue(parameterViewModel == SelectedParameterViewModels[SelectedParameterViewModel]);

        parameterViewModel.Distribution = parameterState
          .GetDistribution()
          .ToString(parameterState.Name);
      }

      void None()
      {
        var parameterViewModel = SelectedParameterViewModels[SelectedParameterViewModel];
        SelectedParameterViewModels.Remove(parameterViewModel);
        parameterViewModel.IsSelected = false;
        _moduleState.ParametersState.SelectedParameter = default;

        var index = _moduleState.ParameterStates.FindIndex(
          ps => ps.Name == parameterViewModel.Name
          );
        var parameterState = _moduleState.ParameterStates[index];
        RequireTrue(parameterState.IsSelected);
        parameterState = parameterState.WithIsSelected(false);
        _moduleState.ParameterStates = _moduleState.ParameterStates.SetItem(
          index, 
          parameterState
          );
      }

      _parameterDistributionViewModel.ParameterState.Match(Some, None);
    }

    private void ObserveModuleStateParameterStateChange(
      (Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier) change
      )
    {
      if (!change.ObservableQualifier.IsAddOrChange()) return;

      var selectedParameterViewModel = SelectedParameterViewModel.IsFound()
        ? SelectedParameterViewModels[SelectedParameterViewModel]
        : default;

      change.ParameterStates.Iter(ps =>
      {
        var parameterViewModel = AllParameterViewModels
          .Find(vm => vm.Name == ps.Name)
          .AssertSome();

        if (ps.IsSelected)
        {
          if (!SelectedParameterViewModels.Contains(parameterViewModel))
          {
            RequireFalse(parameterViewModel.IsSelected);
            parameterViewModel.IsSelected = true;
            SelectedParameterViewModels.InsertInOrdered(
              parameterViewModel, 
              pvm => pvm.SortKey
              );
          }

          parameterViewModel.Distribution = ps.GetDistribution().ToString(ps.Name);

          if (parameterViewModel == selectedParameterViewModel)
          {
            ParameterDistributionViewModel.ParameterState = ps;
          }
        }
        else
        {
          if (SelectedParameterViewModels.Contains(parameterViewModel))
          {
            RequireTrue(parameterViewModel.IsSelected);
            SelectedParameterViewModels.Remove(parameterViewModel);
            parameterViewModel.IsSelected = false;
          }

          parameterViewModel.Distribution = default;

          if (parameterViewModel == selectedParameterViewModel)
          {
            ParameterDistributionViewModel.ParameterState = None;
          }
        }
      });
    }

    private void UpdateSelectedParameter()
    {
      if (SelectedParameterViewModel.IsFound())
      {
        var parameterViewModel = SelectedParameterViewModels[SelectedParameterViewModel];
        var parameterState = _moduleState.ParameterStates
          .Find(ps => ps.Name == parameterViewModel.Name)
          .AssertSome();
        ParameterDistributionViewModel.ParameterState = parameterState;
        _moduleState.ParametersState.SelectedParameter = parameterState.Name;
      }
      else
      {
        ParameterDistributionViewModel.ParameterState = None;
        _moduleState.ParametersState.SelectedParameter = default;
      }
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
          _parameterDistributionViewModel.Dispose();
        }

        _disposed = true;
      }
    }

    private readonly Simulation _simulation;
    private readonly ModuleState _moduleState;
    private readonly ICommand _toggleSelectParameter;
    private readonly ParameterDistributionViewModel _parameterDistributionViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
