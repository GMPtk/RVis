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

namespace Estimation
{
  internal sealed class PriorsViewModel : IPriorsViewModel, INotifyPropertyChanged, IDisposable
  {
    internal PriorsViewModel(IAppState appState, IAppService appService, IAppSettings appSettings, ModuleState moduleState)
    {
      _simulation = appState.Target.AssertSome("No simulation");
      _moduleState = moduleState;

      _toggleSelectPrior = ReactiveCommand.Create<IPriorViewModel>(HandleToggleSelectPrior);

      var parameters = _simulation.SimConfig.SimInput.SimParameters;

      AllPriorViewModels = parameters
        .Map(p => new PriorViewModel(p.Name, _toggleSelectPrior))
        .OrderBy(vm => vm.SortKey)
        .ToArr<IPriorViewModel>();

      moduleState.PriorStates.Iter(ps =>
      {
        var priorViewModel = AllPriorViewModels
        .Find(vm => vm.Name == ps.Name)
        .AssertSome($"Unknown prior in module state: {ps.Name}");

        priorViewModel.IsSelected = ps.IsSelected;
        priorViewModel.Distribution = ps.GetDistribution().ToString(ps.Name);
      });

      SelectedPriorViewModels = new ObservableCollection<IPriorViewModel>(
        AllPriorViewModels.Where(vm => vm.IsSelected)
        );

      SelectedPriorViewModel = SelectedPriorViewModels.FindIndex(
        vm => vm.Name == moduleState.PriorsState.SelectedPrior
        );

      var distributionTypes =
        DistributionType.Beta |
        DistributionType.Gamma |
        DistributionType.Invariant |
        DistributionType.LogNormal |
        DistributionType.Normal |
        DistributionType.Uniform
        ;

      _parameterDistributionViewModel = new ParameterDistributionViewModel(
        appState,
        appService,
        appSettings,
        distributionTypes,
        allowTruncation: true
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        this
          .WhenAny(vm => vm.SelectedPriorViewModel, _ => default(object))
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object?>(
              ObserveSelectedPriorViewModel
              )
            ),

        _parameterDistributionViewModel
          .ObservableForProperty(vm => vm.ParameterState)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveParameterDistributionViewModelParameterState
              )
            ),

        moduleState.PriorStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<ParameterState>, ObservableQualifier)>(
            ObserveModuleStatePriorStateChange
            )
          )

      );
    }

    public Arr<IPriorViewModel> AllPriorViewModels { get; }

    public ObservableCollection<IPriorViewModel> SelectedPriorViewModels { get; }

    public int SelectedPriorViewModel
    {
      get => _selectedPriorViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedPriorViewModel, value, PropertyChanged);
    }
    private int _selectedPriorViewModel;

    public IParameterDistributionViewModel ParameterDistributionViewModel => _parameterDistributionViewModel;

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value, PropertyChanged);
    }
    private bool _isVisible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void HandleToggleSelectPrior(IPriorViewModel priorViewModel)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var index = _moduleState.PriorStates.FindIndex(ps => ps.Name == priorViewModel.Name);
        var isSelected = SelectedPriorViewModels.Contains(priorViewModel);
        ParameterState priorState;

        if (index.IsFound())
        {
          priorState = _moduleState.PriorStates[index];
          priorState = priorState.WithIsSelected(!isSelected);
          _moduleState.PriorStates = _moduleState.PriorStates.SetItem(index, priorState);
        }
        else
        {
          var parameters = _simulation.SimConfig.SimInput.SimParameters;
          var parameter = parameters.GetParameter(priorViewModel.Name);
          priorState = ParameterState.Create(parameter.Name, parameter.Scalar);
          _moduleState.PriorStates += priorState;
        }

        if (isSelected)
        {
          SelectedPriorViewModels.Remove(priorViewModel);
          if (SelectedPriorViewModel.IsntFound())
          {
            _parameterDistributionViewModel.ParameterState = None;
            _moduleState.PriorsState.SelectedPrior = default;
          }
        }
        else
        {
          SelectedPriorViewModels.InsertInOrdered(priorViewModel, pvm => pvm.SortKey);
          priorViewModel.Distribution = priorState.GetDistribution().ToString(priorViewModel.Name);
        }

        priorViewModel.IsSelected = !isSelected;
      }
    }

    private void ObserveSelectedPriorViewModel(object? _)
    {
      if (SelectedPriorViewModel.IsFound())
      {
        var priorViewModel = SelectedPriorViewModels[SelectedPriorViewModel];
        var priorState = _moduleState.PriorStates
          .Find(ps => ps.Name == priorViewModel.Name)
          .AssertSome();
        ParameterDistributionViewModel.ParameterState = priorState;
        _moduleState.PriorsState.SelectedPrior = priorState.Name;
      }
      else
      {
        ParameterDistributionViewModel.ParameterState = None;
        _moduleState.PriorsState.SelectedPrior = default;
      }
    }

    private void ObserveParameterDistributionViewModelParameterState(object _)
    {
      void Some(ParameterState priorState)
      {
        var index = _moduleState.PriorStates.FindIndex(ps => ps.Name == priorState.Name);
        _moduleState.PriorStates = _moduleState.PriorStates.SetItem(index, priorState);

        var priorViewModel = AllPriorViewModels
          .Find(pvm => pvm.Name == priorState.Name)
          .AssertSome();

        RequireTrue(priorViewModel.IsSelected);
        RequireTrue(priorViewModel == SelectedPriorViewModels[SelectedPriorViewModel]);

        priorViewModel.Distribution = priorState.GetDistribution().ToString(priorState.Name);
      }

      void None()
      {
        var priorViewModel = SelectedPriorViewModels[SelectedPriorViewModel];
        SelectedPriorViewModels.Remove(priorViewModel);
        priorViewModel.IsSelected = false;
        _moduleState.PriorsState.SelectedPrior = default;

        var index = _moduleState.PriorStates.FindIndex(ps => ps.Name == priorViewModel.Name);
        var priorState = _moduleState.PriorStates[index];
        RequireTrue(priorState.IsSelected);
        priorState = priorState.WithIsSelected(false);
        _moduleState.PriorStates = _moduleState.PriorStates.SetItem(index, priorState);
      }

      _parameterDistributionViewModel.ParameterState.Match(Some, None);
    }

    private void ObserveModuleStatePriorStateChange((Arr<ParameterState> PriorStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!change.ObservableQualifier.IsAddOrChange()) return;

      var selectedPriorViewModel = SelectedPriorViewModel.IsFound()
        ? SelectedPriorViewModels[SelectedPriorViewModel]
        : default;

      change.PriorStates.Iter(ps =>
      {
        var priorViewModel = AllPriorViewModels.Find(vm => vm.Name == ps.Name).AssertSome();

        if (ps.IsSelected)
        {
          if (!SelectedPriorViewModels.Contains(priorViewModel))
          {
            RequireFalse(priorViewModel.IsSelected);
            priorViewModel.IsSelected = true;
            SelectedPriorViewModels.InsertInOrdered(priorViewModel, pvm => pvm.SortKey);
          }

          priorViewModel.Distribution = ps.GetDistribution().ToString(ps.Name);

          if (priorViewModel == selectedPriorViewModel)
          {
            ParameterDistributionViewModel.ParameterState = ps;
          }
        }
        else
        {
          if (SelectedPriorViewModels.Contains(priorViewModel))
          {
            RequireTrue(priorViewModel.IsSelected);
            SelectedPriorViewModels.Remove(priorViewModel);
            priorViewModel.IsSelected = false;
          }

          priorViewModel.Distribution = default;

          if (priorViewModel == selectedPriorViewModel)
          {
            ParameterDistributionViewModel.ParameterState = None;
          }
        }
      });
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
    private readonly ICommand _toggleSelectPrior;
    private readonly ParameterDistributionViewModel _parameterDistributionViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
