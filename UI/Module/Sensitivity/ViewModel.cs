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
using System.IO;
using System.Reactive.Disposables;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static Sensitivity.Logger;
using static System.IO.Path;

namespace Sensitivity
{
  internal sealed class ViewModel :
    IViewModel,
    ITaskRunnerContainer,
    ISharedStateProvider,
    ICommonConfiguration,
    IExportedDataProvider,
    IDisposable
  {
    internal ViewModel(IAppState appState, IAppService appService, IAppSettings appSettings)
    {
      _appState = appState;
      _appService = appService;
      _simulation = appState.Target.AssertSome();

      var pathToSensitivityDesignsDirectory = _simulation.GetPrivateDirectory(
        nameof(Sensitivity),
        nameof(SensitivityDesigns)
        );
      _sensitivityDesigns = new SensitivityDesigns(pathToSensitivityDesignsDirectory);

      _moduleState = ModuleState.LoadOrCreate(_simulation, _sensitivityDesigns);

      _parametersViewModel = new ParametersViewModel(appState, appService, appSettings, _moduleState);
      _designViewModel = new DesignViewModel(appState, appService, appSettings, _moduleState, _sensitivityDesigns);
      _morrisMeasuresViewModel = new MorrisMeasuresViewModel(appState, appService, appSettings, _moduleState, _sensitivityDesigns);
      _morrisEffectsViewModel = new MorrisEffectsViewModel(appState, appService, appSettings, _moduleState);
      _fast99MeasuresViewModel = new Fast99MeasuresViewModel(appState, appService, appSettings, _moduleState, _sensitivityDesigns);
      _fast99EffectsViewModel = new Fast99EffectsViewModel(appState, appService, appSettings, _moduleState);
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
              ObserveTargetSensitivityDesign
              )
          ),

        _moduleState
          .ObservableForProperty(ms => ms.SensitivityDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSensitivityDesign
              )
            ),

        _sensitivityDesigns.SensitivityDesignChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(DesignDigest, ObservableQualifier)>(
            ObserveSensitivityDesignChange
            )
          )

        );

      SetActivities();

      if (_moduleState.SensitivityDesign != default) _designViewModel.IsSelected = true;
    }

    public IParametersViewModel ParametersViewModel => _parametersViewModel;

    public IDesignViewModel DesignViewModel => _designViewModel;

    public IMorrisMeasuresViewModel MorrisMeasuresViewModel => _morrisMeasuresViewModel;

    public IMorrisEffectsViewModel MorrisEffectsViewModel => _morrisEffectsViewModel;

    public IFast99MeasuresViewModel Fast99MeasuresViewModel => _fast99MeasuresViewModel;

    public IFast99EffectsViewModel Fast99EffectsViewModel => _fast99EffectsViewModel;

    public IDesignDigestsViewModel DesignDigestsViewModel => _designDigestsViewModel;

    public Arr<ITaskRunner> GetTaskRunners() => Array<ITaskRunner>(_designViewModel);

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
            pss => ApplyDistributionAndSelect(pss.Parameter, p.PS, pss.Minimum, pss.Maximum, pss.Distribution),
            () => p.PS.WithIsSelected(false)
            ));

          var added = parameterSharedStates
            .Filter(pss => !_moduleState.ParameterStates.Exists(ps => ps.Name == pss.Parameter.Name))
            .Map(pss => CreateAndApplyDistribution(pss));

          UnloadDesign();
          SetActivities();

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

          UnloadDesign();
          SetActivities();

          if (index.IsFound())
          {
            var parameterState = ApplyDistributionAndSelect(
              parameterSharedState.Parameter,
              _moduleState.ParameterStates[index],
              parameterSharedState.Minimum,
              parameterSharedState.Maximum,
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
          var (value, minimum, maximum) =
            _appState.SimSharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
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

    public DataExportConfiguration GetDataExportConfiguration(
      string rootExportDirectory
      )
    {
      if (_moduleState.SensitivityDesign == default)
      {
        throw new InvalidOperationException("Load a sensitivity design");
      }

      var title = $"Export from {nameof(Sensitivity)}: {_moduleState.SensitivityDesign.CreatedOn.ToDirectoryName()}";

      if (_moduleState.RootExportDirectory.IsAString())
      {
        rootExportDirectory = _moduleState.RootExportDirectory;
      }
      else
      {
        rootExportDirectory = Combine(
          rootExportDirectory,
          nameof(Sensitivity).ToLowerInvariant(),
          _simulation.SimConfig.Title
          );
      }

      var exportDirectoryName = _moduleState.SensitivityDesign.CreatedOn.ToDirectoryName();

      var openAfterExport = _moduleState.OpenAfterExport;

      var outputNames = _moduleState.SensitivityDesign.SensitivityMethod == SensitivityMethod.Morris
        ? _moduleState.MeasuresState.MorrisOutputMeasures.Keys.ToArr()
        : _moduleState.MeasuresState.Fast99OutputMeasures.Keys.ToArr();

      var outputs = outputNames.Map(on => (on, true));

      return new DataExportConfiguration(
        title,
        rootExportDirectory,
        exportDirectoryName,
        openAfterExport,
        outputs
        );
    }

    public void ExportData(DataExportConfiguration dataExportConfiguration)
    {
      var targetDirectory = Combine(
        dataExportConfiguration.RootExportDirectory,
        dataExportConfiguration.ExportDirectoryName
        );

      if (!Directory.Exists(targetDirectory))
      {
        Directory.CreateDirectory(targetDirectory);
      }

      var outputNames = dataExportConfiguration.Outputs
        .Filter(o => o.IncludeInExport)
        .Map(o => o.Name);

      _designViewModel.Export(outputNames, targetDirectory);

      _moduleState.RootExportDirectory = dataExportConfiguration.RootExportDirectory;
      _moduleState.OpenAfterExport = dataExportConfiguration.OpenAfterExport;
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
          _fast99EffectsViewModel.Dispose();
          _fast99MeasuresViewModel.Dispose();
          _morrisEffectsViewModel.Dispose();
          _morrisMeasuresViewModel.Dispose();
          _designViewModel.Dispose();
          _parametersViewModel.Dispose();
          _moduleState.Dispose();

          ModuleState.Save(_moduleState, _simulation);
        }
        _disposed = true;
      }
    }

    private void ObserveParameterStateChange(
      (Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier) change
      )
    {
      var isUnloading = _moduleState.SensitivityDesign != default;

      UnloadDesign();
      SetActivities();

      if (isUnloading) _designViewModel.IsSelected = true;

      if (!_moduleState.AutoShareParameterSharedState.IsTrue()) return;

      if (change.ObservableQualifier.IsAddOrChange())
      {
        change.ParameterStates
          .Filter(ps => ps.IsSelected)
          .ShareStates(_appState);
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        _appState.SimSharedState.UnshareParameterState(change.ParameterStates.Map(ps => ps.Name));
      }
    }

    private void ObserveParameterSharedStateChange(
      (Arr<SimParameterSharedState> ParameterSharedStates, ObservableQualifier ObservableQualifier) change
      )
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
              ps => ApplyDistributionAndSelect(ps, pss.Value, pss.Minimum, pss.Maximum, pss.Distribution),
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

    private void ObserveTargetSensitivityDesign(object _) =>
      _designDigestsViewModel.TargetSensitivityDesign.IfSome(
        t => LoadSensitivityDesign(t.CreatedOn)
        );

    private void ObserveModuleStateSensitivityDesign(object _)
    {
      SetActivities();
    }

    private void ObserveSensitivityDesignChange(
      (DesignDigest DesignDigest, ObservableQualifier ObservableQualifier) change
      )
    {
      if (change.ObservableQualifier.IsRemove())
      {
        if (change.DesignDigest.CreatedOn == _moduleState.SensitivityDesign?.CreatedOn)
        {
          UnloadDesign();
          SetActivities();
        }
      }
    }

    private static ParameterState ApplyDistributionAndSelect(
      SimParameter parameter,
      ParameterState parameterState,
      double minimum,
      double maximum,
      Option<IDistribution> maybeDistribution
      ) =>
      ApplyDistributionAndSelect(parameterState, parameter.Scalar, minimum, maximum, maybeDistribution);

    private static ParameterState ApplyDistributionAndSelect(
      ParameterState parameterState,
      double value,
      double minimum,
      double maximum,
      Option<IDistribution> maybeDistribution
      )
    {
      var distributions = parameterState.Distributions.Map(
              d => d.WithLowerUpper(minimum, maximum)
              );
      var invariantDistribution = new InvariantDistribution(value);
      distributions = distributions.SetDistribution(invariantDistribution);

      DistributionType distributionType;

      (distributionType, distributions) = maybeDistribution.Match(
         d => (d.DistributionType, distributions.SetDistribution(d)),
         () => (parameterState.DistributionType, distributions)
         );

      return new ParameterState(
        parameterState.Name,
        distributionType,
        distributions,
        isSelected: true
        );
    }

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
        isSelected: true
        );

      return parameterState;
    }

    private void LoadSensitivityDesign(DateTime createdOn)
    {
      try
      {
        var sensitivityDesign = _sensitivityDesigns.Load(createdOn);
        var trace = _sensitivityDesigns.LoadTrace(sensitivityDesign);
        var ranking = _sensitivityDesigns.LoadRanking(sensitivityDesign);

        var existingParameterStates = _moduleState.ParameterStates.Map(
          ps => sensitivityDesign.DesignParameters
            .Find(dp => dp.Name == ps.Name)
            .Match(
              dp =>
              {
                var index = ps.Distributions.FindIndex(
                  e => e.DistributionType == dp.Distribution.DistributionType
                  );
                var distributions = ps.Distributions.SetItem(index, dp.Distribution);
                return new ParameterState(
                  ps.Name,
                  dp.Distribution.DistributionType,
                  distributions,
                  isSelected: true
                  );
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
              isSelected: true
              );

            return parameterState;
          });

        _moduleState.ParameterStates = existingParameterStates + newParameterStates;

        UnloadDesign();

        _moduleState.Ranking = ranking;
        _moduleState.Trace = trace;
        _moduleState.SensitivityDesign = sensitivityDesign;

        SetActivities();

        _designViewModel.IsSelected = true;
      }
      catch (Exception ex)
      {
        _appService.Notify(
          nameof(ViewModel),
          nameof(LoadSensitivityDesign),
          ex
          );
        Log.Error(ex);
      }
    }

    private void UnloadDesign()
    {
      _moduleState.SensitivityDesign = default;
      _moduleState.Trace = default;
      _moduleState.MeasuresState.MorrisOutputMeasures = default;
      _moduleState.MeasuresState.Fast99OutputMeasures = default;
      _moduleState.Ranking = default;
    }

    private void SetActivities()
    {
      if (_moduleState.SensitivityDesign is null)
      {
        // in design mode
        _parametersViewModel.IsVisible = true;
        _morrisMeasuresViewModel.IsVisible = false;
        _morrisEffectsViewModel.IsVisible = false;
        _fast99MeasuresViewModel.IsVisible = false;
        _fast99EffectsViewModel.IsVisible = false;
        return;
      }

      var isInMorrisMode = _moduleState.SensitivityDesign.SensitivityMethod == SensitivityMethod.Morris;
      _morrisMeasuresViewModel.IsVisible = isInMorrisMode;
      _morrisEffectsViewModel.IsVisible = isInMorrisMode;

      var isInFast99Mode = _moduleState.SensitivityDesign.SensitivityMethod == SensitivityMethod.Fast99;
      _fast99MeasuresViewModel.IsVisible = isInFast99Mode;
      _fast99EffectsViewModel.IsVisible = isInFast99Mode;
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly Simulation _simulation;
    private readonly SensitivityDesigns _sensitivityDesigns;
    private readonly ModuleState _moduleState;
    private readonly ParametersViewModel _parametersViewModel;
    private readonly DesignViewModel _designViewModel;
    private readonly MorrisMeasuresViewModel _morrisMeasuresViewModel;
    private readonly MorrisEffectsViewModel _morrisEffectsViewModel;
    private readonly Fast99MeasuresViewModel _fast99MeasuresViewModel;
    private readonly Fast99EffectsViewModel _fast99EffectsViewModel;
    private readonly DesignDigestsViewModel _designDigestsViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
