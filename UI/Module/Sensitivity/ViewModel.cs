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
using System.Linq;
using System.Reactive.Disposables;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static Sensitivity.Logger;

namespace Sensitivity
{
  internal sealed class ViewModel : IViewModel, ITaskRunnerContainer, ISharedStateProvider, ICommonConfiguration, IDisposable
  {
    internal ViewModel(IAppState appState, IAppService appService, IAppSettings appSettings)
    {
      _appState = appState;
      _appService = appService;
      _simulation = appState.Target.AssertSome();

      var pathToSensitivityDesignsDirectory = _simulation.GetPrivateDirectory(nameof(Sensitivity), nameof(SensitivityDesigns));
      _sensitivityDesigns = new SensitivityDesigns(pathToSensitivityDesignsDirectory);

      _moduleState = ModuleState.LoadOrCreate(_simulation, _sensitivityDesigns);

      _parametersViewModel = new ParametersViewModel(appState, appService, appSettings, _moduleState);
      _designViewModel = new DesignViewModel(appState, appService, appSettings, _moduleState, _sensitivityDesigns);
      _varianceViewModel = new VarianceViewModel(appState, appService, appSettings, _moduleState, _sensitivityDesigns);
      _effectsViewModel = new EffectsViewModel(appState, appService, appSettings, _moduleState, _sensitivityDesigns);
      _designDigestsViewModel = new DesignDigestsViewModel(appService, _moduleState, _sensitivityDesigns);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _moduleState.ParameterStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<ParameterState>, ObservableQualifier)>(
            ObserveParameterStateChange
            )
          ),

        _appState.SimSharedState.ParameterSharedStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimParameterSharedState>, ObservableQualifier)>(
            ObserveParameterSharedStateChange
            )
          ),

        _designDigestsViewModel
          .ObservableForProperty(vm => vm.TargetSensitivityDesign).Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveTargetSensitivityDesignCreatedOn
              )
          ),

        _sensitivityDesigns.SensitivityDesignChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(DesignDigest, ObservableQualifier)>(
            ObserveSensitivityDesignChange
            )
          )

        );

      if (_moduleState.SensitivityDesign != default) _designViewModel.IsSelected = true;
    }

    public IParametersViewModel ParametersViewModel => _parametersViewModel;

    public IDesignViewModel DesignViewModel => _designViewModel;

    public IVarianceViewModel VarianceViewModel => _varianceViewModel;

    public IEffectsViewModel EffectsViewModel => _effectsViewModel;

    public IDesignDigestsViewModel DesignDigestsViewModel => _designDigestsViewModel;

    public Arr<ITaskRunner> GetTaskRunners() => Array<ITaskRunner>(_designViewModel, _varianceViewModel, _effectsViewModel);

    public void ApplyState(
      SimSharedStateApply applyType,
      Arr<(SimParameter Parameter, double Minimum, double Maximum, Option<IDistribution> Distribution)> parameterSharedStates,
      Arr<SimElement> elementSharedStates,
      Arr<SimObservations> observationsSharedStates
      )
    {
      if (!applyType.IncludesParameters()) return;

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        if (applyType.IsSet())
        {
          var paired = _moduleState.ParameterStates.Map(
              ps => (PS: ps, PSS: parameterSharedStates.Find(pss => pss.Parameter.Name == ps.Name))
              );

          var updated = paired.Map(p => p.PSS.Match(
            pss => ApplyDistributionAndSelect(pss.Parameter, p.PS, pss.Distribution),
            () => p.PS.WithIsSelected(false)
            ));

          var added = parameterSharedStates
            .Filter(pss => !_moduleState.ParameterStates.Exists(ps => ps.Name == pss.Parameter.Name))
            .Map(pss => CreateAndApplyDistribution(pss));

          _moduleState.ParameterStates = (updated + added)
            .OrderBy(ps => ps.Name.ToUpperInvariant())
            .ToArr();
        }
        else if (applyType.IsSingle())
        {
          var parameterSharedState = parameterSharedStates.Head();

          var index = _moduleState.ParameterStates.FindIndex(
            ps => ps.Name == parameterSharedState.Parameter.Name
            );

          if (index.IsFound())
          {
            var parameterState = ApplyDistributionAndSelect(
              parameterSharedState.Parameter,
              _moduleState.ParameterStates[index],
              parameterSharedState.Distribution
              );
            _moduleState.ParameterStates = _moduleState.ParameterStates.SetItem(index, parameterState);
          }
          else
          {
            var parameterState = CreateAndApplyDistribution(parameterSharedState);
            var parameterStates = _moduleState.ParameterStates + parameterState;
            _moduleState.ParameterStates = parameterStates
              .OrderBy(ps => ps.Name.ToUpperInvariant())
              .ToArr();
          }
        }
      }
    }

    public void ShareState(ISimSharedStateBuilder sharedStateBuilder)
    {
      if (!sharedStateBuilder.BuildType.IncludesParameters()) return;

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _moduleState.ParameterStates.Filter(ps => ps.IsSelected).Iter(ps =>
        {
          var (value, minimum, maximum) = _appState.SimSharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
            ps.Name,
            _simulation.SimConfig.SimInput.SimParameters
            );

          var distribution = ps.GetDistribution();

          if (distribution.DistributionType == DistributionType.Invariant)
          {
            var invariantDistribution = RequireInstanceOf<InvariantDistribution>(distribution);
            value = invariantDistribution.Value;
            if (minimum > value) minimum = value.GetPreviousOrderOfMagnitude();
            if (maximum < value) maximum = value.GetNextOrderOfMagnitude();
            sharedStateBuilder.AddParameter(ps.Name, value, minimum, maximum, None);
          }
          else
          {
            sharedStateBuilder.AddParameter(ps.Name, value, minimum, maximum, Some(distribution));
          }
        });
      }
    }

    bool? ICommonConfiguration.AutoApplyParameterSharedState
    {
      get => _moduleState.AutoApplyParameterSharedState;
      set => _moduleState.AutoApplyParameterSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareParameterSharedState
    {
      get => _moduleState.AutoShareParameterSharedState;
      set => _moduleState.AutoShareParameterSharedState = value;
    }

    bool? ICommonConfiguration.AutoApplyElementSharedState
    {
      get => _moduleState.AutoApplyElementSharedState;
      set => _moduleState.AutoApplyElementSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareElementSharedState
    {
      get => _moduleState.AutoShareElementSharedState;
      set => _moduleState.AutoShareElementSharedState = value;
    }

    bool? ICommonConfiguration.AutoApplyObservationsSharedState
    {
      get => _moduleState.AutoApplyObservationsSharedState;
      set => _moduleState.AutoApplyObservationsSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareObservationsSharedState
    {
      get => _moduleState.AutoShareObservationsSharedState;
      set => _moduleState.AutoShareObservationsSharedState = value;
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
          _designDigestsViewModel.Dispose();
          _effectsViewModel.Dispose();
          _varianceViewModel.Dispose();
          _designViewModel.Dispose();
          _parametersViewModel.Dispose();
          _moduleState.Dispose();

          ModuleState.Save(_moduleState, _simulation);
        }
        _disposed = true;
      }
    }

    private void ObserveParameterStateChange((Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoShareParameterSharedState.IsTrue()) return;

      if (change.ObservableQualifier.IsAddOrChange())
      {
        var states = change.ParameterStates
          .Filter(ps => ps.IsSelected)
          .Map(ps =>
          {
            var (value, minimum, maximum) = _appState.SimSharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
              ps.Name,
              _simulation.SimConfig.SimInput.SimParameters
              );

            var distribution = ps.GetDistribution();

            if (distribution.DistributionType == DistributionType.Invariant)
            {
              var invariantDistribution = RequireInstanceOf<InvariantDistribution>(distribution);
              value = invariantDistribution.Value;
              if (minimum > value) minimum = value.GetPreviousOrderOfMagnitude();
              if (maximum < value) maximum = value.GetNextOrderOfMagnitude();
              return (ps.Name, value, minimum, maximum, None);
            }

            return (ps.Name, value, minimum, maximum, Some(ps.GetDistribution()));
          });

        _appState.SimSharedState.ShareParameterState(states);
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        _appState.SimSharedState.UnshareParameterState(change.ParameterStates.Map(ps => ps.Name));
      }
    }

    private void ObserveParameterSharedStateChange((Arr<SimParameterSharedState> ParameterSharedStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoApplyParameterSharedState.IsTrue()) return;

      if (change.ObservableQualifier.IsAddOrChange())
      {
        var notPartOfChange = _moduleState.ParameterStates.Filter(
          ps => !change.ParameterSharedStates.Exists(pss => pss.Name == ps.Name)
          );

        var changes = change.ParameterSharedStates
          .Map(pss => _moduleState.ParameterStates
            .Find(ps => ps.Name == pss.Name)
            .Match(
              ps => ApplyDistributionAndSelect(ps, pss.Value, pss.Distribution),
              () => CreateAndApplyDistribution(pss)
            )
          );

        var parameterStates = notPartOfChange + changes;

        RequireUniqueElements(parameterStates, ps => ps.Name);

        _moduleState.ParameterStates = parameterStates
          .OrderBy(ps => ps.Name.ToUpperInvariant())
          .ToArr();
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        var remaining = _moduleState.ParameterStates.Filter(
          ps => !change.ParameterSharedStates.Exists(pss => pss.Name == ps.Name)
          );

        if (remaining.Count < _moduleState.ParameterStates.Count)
        {
          _moduleState.ParameterStates = remaining;
        }
      }
    }

    private void ObserveTargetSensitivityDesignCreatedOn(object _) => 
      _designDigestsViewModel.TargetSensitivityDesign.IfSome(t => LoadSensitivityDesign(t.CreatedOn));

    private void ObserveSensitivityDesignChange((DesignDigest DesignDigest, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier.IsRemove())
      {
        if (change.DesignDigest.CreatedOn == _moduleState.SensitivityDesign?.CreatedOn)
        {
          _moduleState.SensitivityDesign = default;
          _moduleState.DesignOutputs = default;
          _moduleState.Trace = default;
          _moduleState.MeasuresState.OutputMeasures = default;
        }
      }
    }

    private static ParameterState ApplyDistributionAndSelect(
      SimParameter parameter,
      ParameterState parameterState,
      Option<IDistribution> maybeDistribution
      ) =>
      ApplyDistributionAndSelect(parameterState, parameter.Scalar, maybeDistribution);

    private static ParameterState ApplyDistributionAndSelect(
      ParameterState parameterState,
      double value,
      Option<IDistribution> maybeDistribution
      ) =>
      maybeDistribution.Match(
        d =>
        {
          var index = parameterState.Distributions.FindIndex(e => e.DistributionType == d.DistributionType);
          var distributions = parameterState.Distributions.SetItem(index, d);
          return new ParameterState(parameterState.Name, d.DistributionType, distributions, true);
        },
        () =>
        {
          var invariantDistribution = new InvariantDistribution(value);
          var distributions = parameterState.Distributions.SetDistribution(invariantDistribution);
          parameterState = new ParameterState(parameterState.Name, DistributionType.Invariant, distributions, true);
          return parameterState;
        });

    private static ParameterState CreateAndApplyDistribution(
      (SimParameter Parameter, double Minimum, double Maximum, Option<IDistribution> Distribution) parameterSharedState
      ) =>
      CreateAndApplyDistribution(
        parameterSharedState.Parameter.Name,
        parameterSharedState.Parameter.Scalar,
        parameterSharedState.Distribution
        );

    private static ParameterState CreateAndApplyDistribution(
      SimParameterSharedState parameterSharedState
      ) =>
      CreateAndApplyDistribution(
        parameterSharedState.Name,
        parameterSharedState.Value,
        parameterSharedState.Distribution
        );

    private static ParameterState CreateAndApplyDistribution(
      string name, double value, Option<IDistribution> maybeDistribution
      )
    {
      var distributions = Distribution.GetDefaults();

      var distribution = maybeDistribution.Match(
        d => d,
        () => new InvariantDistribution(value)
        );

      distributions = distributions.SetDistribution(distribution);

      var parameterState = new ParameterState(
        name,
        distribution.DistributionType,
        distributions,
        true
        );

      return parameterState;
    }

    private void LoadSensitivityDesign(DateTime createdOn)
    {
      try
      {
        var sensitivityDesign = _sensitivityDesigns.Load(createdOn);
        var trace = _sensitivityDesigns.LoadTrace(sensitivityDesign);

        var existingParameterStates = _moduleState.ParameterStates.Map(
          ps => sensitivityDesign.DesignParameters
            .Find(dp => dp.Name == ps.Name)
            .Match(
              dp =>
              {
                var index = ps.Distributions.FindIndex(e => e.DistributionType == dp.Distribution.DistributionType);
                var distributions = ps.Distributions.SetItem(index, dp.Distribution);
                return new ParameterState(ps.Name, dp.Distribution.DistributionType, distributions, true);
              },
              () => ps.WithIsSelected(false))
          );

        var newParameterStates = sensitivityDesign.DesignParameters
          .Filter(dp => !existingParameterStates.Exists(ps => ps.Name == dp.Name))
          .Map(dp =>
          {
            var distributions = Distribution.GetDefaults();

            distributions = distributions.SetDistribution(dp.Distribution);

            var parameterState = new ParameterState(
              dp.Name,
              dp.Distribution.DistributionType,
              distributions,
              true
              );

            return parameterState;
          });

        _moduleState.ParameterStates = existingParameterStates + newParameterStates;

        _moduleState.SensitivityDesign = default;

        _moduleState.DesignOutputs = default;
        _moduleState.MeasuresState.OutputMeasures = default;
        _moduleState.Trace = trace;
        _moduleState.SensitivityDesign = sensitivityDesign;

        if (!_varianceViewModel.IsSelected)
        {
          _designViewModel.IsSelected = true;
        }
      }
      catch (Exception ex)
      {
        _appService.Notify(
          nameof(ViewModel),
          nameof(ObserveTargetSensitivityDesignCreatedOn),
          ex
          );
        Log.Error(ex);
      }
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly Simulation _simulation;
    private readonly SensitivityDesigns _sensitivityDesigns;
    private readonly ModuleState _moduleState;
    private readonly ParametersViewModel _parametersViewModel;
    private readonly DesignViewModel _designViewModel;
    private readonly VarianceViewModel _varianceViewModel;
    private readonly EffectsViewModel _effectsViewModel;
    private readonly DesignDigestsViewModel _designDigestsViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
