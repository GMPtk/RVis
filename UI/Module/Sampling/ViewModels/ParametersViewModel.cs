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
using static System.Double;

namespace Sampling
{
  internal sealed class ParametersViewModel : IParametersViewModel, INotifyPropertyChanged, IDisposable
  {
    internal ParametersViewModel(IAppState appState, IAppService appService, IAppSettings appSettings, ModuleState moduleState)
    {
      _appState = appState;
      _appService = appService;
      _simulation = appState.Target.AssertSome("No simulation");
      _moduleState = moduleState;

      _toggleSelectParameter = ReactiveCommand.Create<IParameterViewModel>(HandleToggleSelectParameter);

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
      });

      SelectedParameterViewModels = new ObservableCollection<IParameterViewModel>(
        AllParameterViewModels.Where(vm => vm.IsSelected)
        );

      SelectedParameterViewModel = SelectedParameterViewModels.FindIndex(
        vm => vm.Name == moduleState.ParametersState.SelectedParameter
        );

      _latinHypercubeDesignType = _moduleState.LatinHypercubeDesign.LatinHypercubeDesignType;

      ConfigureLHS = ReactiveCommand.Create(
        HandleConfigureLHS,
        this.WhenAny(vm => vm.CanConfigureLHS, _ => CanConfigureLHS)
        );

      _parameterDistributionViewModel = new ParameterDistributionViewModel(appState, appService, appSettings, true);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _subscriptions = new CompositeDisposable(

          this
            .WhenAny(vm => vm.SelectedParameterViewModel, _ => default(object))
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<object>(
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

        UpdateParameterViewModelDistributions(moduleState.ParameterStates);
        UpdateSelectedParameter();
        UpdateEnable();
      }
    }

    public Arr<IParameterViewModel> AllParameterViewModels { get; }

    public ObservableCollection<IParameterViewModel> SelectedParameterViewModels { get; }

    public bool CanConfigureLHS
    {
      get => _canConfigureLHS;
      set => this.RaiseAndSetIfChanged(ref _canConfigureLHS, value, PropertyChanged);
    }
    private bool _canConfigureLHS;

    public ICommand ConfigureLHS { get; }

    public int SelectedParameterViewModel
    {
      get => _selectedParameterViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedParameterViewModel, value, PropertyChanged);
    }
    private int _selectedParameterViewModel;

    public IParameterDistributionViewModel ParameterDistributionViewModel => _parameterDistributionViewModel;

    public LatinHypercubeDesignType LatinHypercubeDesignType
    {
      get => _latinHypercubeDesignType;
      set => this.RaiseAndSetIfChanged(ref _latinHypercubeDesignType, value, PropertyChanged);
    }
    private LatinHypercubeDesignType _latinHypercubeDesignType;

    public event PropertyChangedEventHandler PropertyChanged;

    public void Dispose() => Dispose(true);

    private void HandleToggleSelectParameter(IParameterViewModel parameterViewModel)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var index = _moduleState.ParameterStates.FindIndex(ps => ps.Name == parameterViewModel.Name);
        var isSelected = SelectedParameterViewModels.Contains(parameterViewModel);
        ParameterState parameterState;

        if (index.IsFound())
        {
          parameterState = _moduleState.ParameterStates[index];
          parameterState = parameterState.WithIsSelected(!isSelected);
          _moduleState.ParameterStates = _moduleState.ParameterStates.SetItem(index, parameterState);
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
          parameterViewModel.Distribution = parameterState.GetDistribution().ToString(parameterViewModel.Name);
        }

        parameterViewModel.IsSelected = !isSelected;

        UpdateEnable();
      }
    }

    private void HandleConfigureLHS()
    {
      var view = new LHSConfigurationDialog();

      var latinHypercubeDesign = _moduleState.LatinHypercubeDesign;
      var variables = _moduleState.ParameterStates.Filter(ps => ps.IsSelected).Map(ps =>
      {
        double lower;
        double upper;

        if (ps.DistributionType == DistributionType.Invariant)
        {
          var distribution = RequireInstanceOf<InvariantDistribution>(ps.GetDistribution());

          lower = distribution.Value;
          upper = lower;
        }
        else
        {
          var distribution = RequireInstanceOf<UniformDistribution>(ps.GetDistribution(DistributionType.Uniform));

          lower = distribution.Lower;
          upper = distribution.Upper;
        }

        var requiresInitialization =
          IsNaN(lower) || IsInfinity(lower) ||
          IsNaN(upper) || IsInfinity(upper);

        if (requiresInitialization)
        {
          var parameter = _simulation.SimConfig.SimInput.SimParameters.GetParameter(ps.Name);
          var value = parameter.Scalar;

          if (IsNaN(lower) || IsInfinity(lower))
          {
            lower = value.GetPreviousOrderOfMagnitude();
          }

          if (IsNaN(upper) || IsInfinity(upper))
          {
            upper = value.GetNextOrderOfMagnitude();
          }
        }

        return (ps.Name, lower, upper);
      });

      var viewModel = new LHSConfigurationViewModel(_appState, _appService)
      {
        Variables = variables,
        LatinHypercubeDesign = latinHypercubeDesign
      };

      if (viewModel.LatinHypercubeDesignType == LatinHypercubeDesignType.None)
      {
        viewModel.LatinHypercubeDesignType = LatinHypercubeDesignType.Randomized;
      }

      var didOK = _appService.ShowDialog(view, viewModel, default);

      if (didOK)
      {
        _moduleState.LatinHypercubeDesign = viewModel.LatinHypercubeDesign;
        LatinHypercubeDesignType = _moduleState.LatinHypercubeDesign.LatinHypercubeDesignType;

        var isEnable = LatinHypercubeDesignType != LatinHypercubeDesignType.None;

        if (isEnable)
        {
          var parameterStates = _moduleState.ParameterStates.Map(ps =>
          {
            if (!ps.IsSelected) return ps;

            var (_, lower, upper) = viewModel.Variables
              .Find(v => v.Parameter == ps.Name)
              .AssertSome();

            if (lower == upper)
            {
              var index = ps.Distributions.FindIndex(
                d => d.DistributionType == DistributionType.Invariant
                );

              RequireTrue(index.IsFound());

              var distributions = ps.Distributions.SetItem(
                index,
                new InvariantDistribution(lower)
                );

              return new ParameterState(
                ps.Name,
                DistributionType.Invariant,
                distributions,
                ps.IsSelected
                );
            }
            else
            {
              var index = ps.Distributions.FindIndex(
                d => d.DistributionType == DistributionType.Uniform
                );

              RequireTrue(index.IsFound());

              var distributions = ps.Distributions.SetItem(
                index,
                new UniformDistribution(lower, upper)
                );

              return new ParameterState(
                ps.Name,
                DistributionType.Uniform,
                distributions,
                ps.IsSelected
                );
            }
          });

          _moduleState.ParameterStates = parameterStates;
        }

        UpdateParameterViewModelDistributions(_moduleState.ParameterStates);
      }
    }

    private void ObserveSelectedParameterViewModel(object _)
    {
      UpdateSelectedParameter();
    }

    private void ObserveParameterDistributionViewModelParameterState(object _)
    {
      void Some(ParameterState parameterState)
      {
        var index = _moduleState.ParameterStates.FindIndex(ps => ps.Name == parameterState.Name);
        _moduleState.ParameterStates = _moduleState.ParameterStates.SetItem(index, parameterState);

        var parameterViewModel = AllParameterViewModels
          .Find(pvm => pvm.Name == parameterState.Name)
          .AssertSome();

        RequireTrue(parameterViewModel.IsSelected);
        RequireTrue(parameterViewModel == SelectedParameterViewModels[SelectedParameterViewModel]);

        parameterViewModel.Distribution = parameterState.GetDistribution().ToString(parameterState.Name);
      }

      void None()
      {
        var parameterViewModel = SelectedParameterViewModels[SelectedParameterViewModel];
        SelectedParameterViewModels.Remove(parameterViewModel);
        parameterViewModel.IsSelected = false;
        _moduleState.ParametersState.SelectedParameter = default;

        var index = _moduleState.ParameterStates.FindIndex(ps => ps.Name == parameterViewModel.Name);
        var parameterState = _moduleState.ParameterStates[index];
        RequireTrue(parameterState.IsSelected);
        parameterState = parameterState.WithIsSelected(false);
        _moduleState.ParameterStates = _moduleState.ParameterStates.SetItem(index, parameterState);
      }

      _parameterDistributionViewModel.ParameterState.Match(Some, None);

      UpdateEnable();
    }

    private void UpdateParameterViewModelDistributions(Arr<ParameterState> parameterStates)
    {
      parameterStates
        .Filter(ps => ps.IsSelected)
        .Iter(ps =>
        {
          var parameterViewModel = AllParameterViewModels
            .Find(vm => vm.Name == ps.Name)
            .AssertSome();
          RequireTrue(parameterViewModel.IsSelected);
          parameterViewModel.Distribution = ps.GetDistribution().ToString(ps.Name);
        });
    }

    private void ObserveModuleStateParameterStateChange((Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!change.ObservableQualifier.IsAddOrChange()) return;

      var selectedParameterViewModel = SelectedParameterViewModel.IsFound()
        ? SelectedParameterViewModels[SelectedParameterViewModel]
        : default;

      change.ParameterStates.Iter(ps =>
      {
        var parameterViewModel = AllParameterViewModels.Find(vm => vm.Name == ps.Name).AssertSome();

        if (ps.IsSelected)
        {
          if (!SelectedParameterViewModels.Contains(parameterViewModel))
          {
            RequireFalse(parameterViewModel.IsSelected);
            parameterViewModel.IsSelected = true;
            SelectedParameterViewModels.InsertInOrdered(parameterViewModel, pvm => pvm.SortKey);
          }

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

      UpdateParameterViewModelDistributions(change.ParameterStates);

      UpdateEnable();
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

    private void UpdateEnable()
    {
      CanConfigureLHS = SelectedParameterViewModels.Count > 0;
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

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly Simulation _simulation;
    private readonly ModuleState _moduleState;
    private readonly ICommand _toggleSelectParameter;
    private readonly ParameterDistributionViewModel _parameterDistributionViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
