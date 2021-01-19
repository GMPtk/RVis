using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;

namespace RVisUI.AppInf
{
  public sealed class ParameterDistributionViewModel : IParameterDistributionViewModel, INotifyPropertyChanged, IDisposable
  {
    public ParameterDistributionViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      bool allowTruncation = false
      )
      : this(appState, appService, appSettings, DistributionType.All, allowTruncation)
    {
    }

    public ParameterDistributionViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      DistributionType distributionsInView,
      bool allowTruncation = false
      )
    {
      var simulation = appState.Target.AssertSome();
      _parameters = simulation.SimConfig.SimInput.SimParameters;

      var distributionTypes = Distribution.GetDistributionTypes(distributionsInView);

      var distributionViewModelTypes = distributionTypes
        .Map(
          dt => typeof(IDistributionViewModel).Assembly.GetType($"{nameof(RVisUI)}.{nameof(AppInf)}.{dt}DistributionViewModel")
        )
        .Map(LangExt.AssertNotNull);

      _distributionViewModels = distributionViewModelTypes
        .Select(t => Activator.CreateInstance(t, new object[] { appService, appSettings }))
        .Cast<IDistributionViewModel>()
        .ToArr();

      foreach (var distributionViewModel in _distributionViewModels)
      {
        distributionViewModel.AllowTruncation = allowTruncation;
      }

      var displayNames = distributionViewModelTypes.Select(
        t => t.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? t.Name
        );
      DistributionNames = displayNames.ToArr();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      this.ObservableForProperty(vm => vm.SelectedDistributionName).Subscribe(
        _reactiveSafeInvoke.SuspendAndInvoke<object>(
          ObserveSelectedDistributionName
          )
        );

      _distributionViewModels.Iter(dvm => ((INotifyPropertyChanged)dvm)
        .GetWhenPropertyChanged()
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<string?>(
            pn => ObserveDistributionViewModelProperty(dvm, pn)
          )
        )
      );

      this.ObservableForProperty(vm => vm.ParameterState).Subscribe(
        _reactiveSafeInvoke.SuspendAndInvoke<object>(
          ObserveParameterState
          )
        );
    }

    public Arr<string> DistributionNames { get; }

    public int SelectedDistributionName
    {
      get => _selectedDistributionName;
      set => this.RaiseAndSetIfChanged(ref _selectedDistributionName, value, PropertyChanged);
    }
    private int _selectedDistributionName = NOT_FOUND;

    public IDistributionViewModel? DistributionViewModel
    {
      get => _distributionViewModel;
      set => this.RaiseAndSetIfChanged(ref _distributionViewModel, value, PropertyChanged);
    }
    private IDistributionViewModel? _distributionViewModel;

    public Option<ParameterState> ParameterState
    {
      get => _parameterState;
      set => this.RaiseAndSetIfChanged(ref _parameterState, value, PropertyChanged);
    }
    private Option<ParameterState> _parameterState;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void ObserveSelectedDistributionName(object _)
    {
      RequireTrue(SelectedDistributionName.IsFound());

      var parameterState = _parameterState.AssertSome();

      ShowSelectedDistribution(parameterState.Name, parameterState.Distributions);

      ParameterState = new ParameterState(
        parameterState.Name,
        DistributionViewModel.AssertNotNull().DistributionType,
        parameterState.Distributions,
        parameterState.IsSelected
        );
    }

    private void ObserveDistributionViewModelProperty(IDistributionViewModel distributionViewModel, string? propertyName)
    {
      if (propertyName != nameof(IDistributionViewModel<NormalDistribution>.Distribution)) return;

      var parameterState = _parameterState.AssertSome();
      RequireTrue(parameterState.IsSelected);
      RequireTrue(parameterState.DistributionType == distributionViewModel.DistributionType);

      var index = parameterState.Distributions.FindIndex(
        d => d.DistributionType == distributionViewModel.DistributionType
        );

      RequireTrue(index.IsFound());

      var distributions = parameterState.Distributions.SetItem(
        index,
        distributionViewModel.DistributionUnsafe.AssertNotNull()
        );

      ParameterState = new ParameterState(
        parameterState.Name,
        parameterState.DistributionType,
        distributions,
        parameterState.IsSelected
        );
    }

    private void ObserveParameterState(object _)
    {
      void Some(ParameterState parameterState)
      {
        RequireFalse(parameterState.DistributionType == DistributionType.None);

        var index = _distributionViewModels.FindIndex(
          dvm => dvm.DistributionType == parameterState.DistributionType
          );
        RequireTrue(index.IsFound());
        SelectedDistributionName = index;

        ShowSelectedDistribution(parameterState.Name, parameterState.Distributions);
      }

      void None()
      {
        SelectedDistributionName = NOT_FOUND;
        DistributionViewModel = default;
      }

      _parameterState.Match(Some, None);
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _distributionViewModels.Iter(dvm => dvm.Dispose());
        }

        _disposed = true;
      }
    }

    private void ShowSelectedDistribution(string parameterName, Arr<IDistribution> distributions)
    {
      var parameter = _parameters.GetParameter(parameterName);

      var distributionViewModel = _distributionViewModels[SelectedDistributionName];

      var distribution = distributions
        .Find(d => d.DistributionType == distributionViewModel.DistributionType)
        .AssertSome();

      distributionViewModel.DistributionUnsafe = default;
      distributionViewModel.Variable = parameterName;
      distributionViewModel.Unit = parameter.Unit;
      distributionViewModel.DistributionUnsafe = distribution;

      DistributionViewModel = distributionViewModel;
    }

    private readonly Arr<SimParameter> _parameters;
    private readonly Arr<IDistributionViewModel> _distributionViewModels;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private bool _disposed = false;
  }
}
