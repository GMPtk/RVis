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
using System.Linq;
using System.Reactive.Disposables;
using static Estimation.Logger;
using static Estimation.Properties.Resources;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using static System.String;

namespace Estimation
{
  internal sealed class ViewModel :
    IViewModel,
    ISharedStateProvider,
    ICommonConfiguration,
    IExportedDataProvider,
    IDisposable
  {
    internal ViewModel(IAppState appState, IAppService appService, IAppSettings appSettings)
    {
      _appState = appState;
      _appService = appService;
      _sharedState = appState.SimSharedState;
      _simulation = appState.Target.AssertSome();
      _evidence = appState.SimEvidence;

      var pathToEstimationDesignsDirectory = _simulation.GetPrivateDirectory(nameof(Estimation), nameof(EstimationDesigns));
      _estimationDesigns = new EstimationDesigns(pathToEstimationDesignsDirectory, _evidence);

      _moduleState = ModuleState.LoadOrCreate(_simulation, _evidence, appService, _estimationDesigns);

      _priorsViewModel = new PriorsViewModel(appState, appService, appSettings, _moduleState);
      _likelihoodViewModel = new LikelihoodViewModel(appState, appService, appSettings, _moduleState);
      _designViewModel = new DesignViewModel(appState, appService, appSettings, _moduleState, _estimationDesigns);
      _simulationViewModel = new SimulationViewModel(appState, appService, appSettings, _moduleState, _estimationDesigns);
      _posteriorViewModel = new PosteriorViewModel(appState, appService, appSettings, _moduleState);
      _fitViewModel = new FitViewModel(appState, appService, appSettings, _moduleState);
      _designDigestsViewModel = new DesignDigestsViewModel(appService, _moduleState, _estimationDesigns);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _moduleState.PriorStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<ParameterState>, ObservableQualifier)>(
            ObservePriorStateChange
            )
          ),

        _sharedState.ParameterSharedStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimParameterSharedState>, ObservableQualifier)>(
            ObserveParameterSharedStateChange
            )
          ),

        _moduleState.OutputStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<OutputState>, ObservableQualifier)>(
            ObserveOutputStateChange
            )
          ),

        _sharedState.ElementSharedStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimElementSharedState>, ObservableQualifier)>(
            ObserveElementSharedStateChange
            )
          ),

        _moduleState.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations>, ObservableQualifier)>(
            ObserveModuleStateObservationsChange
            )
          ),

        _sharedState.ObservationsSharedStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservationsSharedState>, ObservableQualifier)>(
            ObserveSharedStateObservationsChange
            )
          ),

        _evidence.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations>, ObservableQualifier)>(
            ObserveEvidenceObservationsChange
            )
          ),

        _designDigestsViewModel
          .ObservableForProperty(vm => vm.TargetEstimationDesign).Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveTargetEstimationDesignCreatedOn
              )
          ),

        _moduleState
          .ObservableForProperty(ms => ms.EstimationDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateEstimationDesign
              )
            ),

        _estimationDesigns.EstimationDesignChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(DesignDigest, ObservableQualifier)>(
            ObserveEstimationDesignChange
            )
          )

      );

      SetActivities();

      if (_moduleState.EstimationDesign != default) _designViewModel.IsSelected = true;
    }

    public IPriorsViewModel PriorsViewModel => _priorsViewModel;

    public ILikelihoodViewModel LikelihoodViewModel => _likelihoodViewModel;

    public IDesignViewModel DesignViewModel => _designViewModel;

    public ISimulationViewModel SimulationViewModel => _simulationViewModel;

    public IPosteriorViewModel PosteriorViewModel => _posteriorViewModel;

    public IFitViewModel FitViewModel => _fitViewModel;

    public IDesignDigestsViewModel DesignDigestsViewModel => _designDigestsViewModel;

    public void ApplyState(
      SimSharedStateApply applyType,
      Arr<(SimParameter Parameter, double Minimum, double Maximum, Option<IDistribution> Distribution)> parameterSharedStates,
      Arr<SimElement> elementSharedStates,
      Arr<SimObservations> observationsSharedStates
      )
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        if (applyType.IncludesParameters())
        {
          if (applyType.IsSet())
          {
            var paired = _moduleState.PriorStates.Map(
              ps => (PS: ps, PSS: parameterSharedStates.Find(pss => pss.Parameter.Name == ps.Name))
              );

            var updated = paired.Map(p => p.PSS.Match(
              pss => ApplyDistributionAndSelect(pss.Parameter, p.PS, pss.Minimum, pss.Maximum, pss.Distribution),
              () => p.PS.WithIsSelected(false)
              ));

            var added = parameterSharedStates
              .Filter(pss => !_moduleState.PriorStates.Exists(ps => ps.Name == pss.Parameter.Name))
              .Map(pss => CreateAndApplyDistribution(pss));

            _moduleState.PriorStates = (updated + added)
              .OrderBy(ps => ps.Name.ToUpperInvariant())
              .ToArr();
          }
          else if (applyType.IsSingle())
          {
            var parameterSharedState = parameterSharedStates.Head();

            var index = _moduleState.PriorStates.FindIndex(
              ps => ps.Name == parameterSharedState.Parameter.Name
              );

            if (index.IsFound())
            {
              var priorState = ApplyDistributionAndSelect(
                parameterSharedState.Parameter,
                _moduleState.PriorStates[index],
                parameterSharedState.Minimum,
                parameterSharedState.Maximum,
                parameterSharedState.Distribution
                );
              _moduleState.PriorStates = _moduleState.PriorStates.SetItem(index, priorState);
            }
            else
            {
              var priorState = CreateAndApplyDistribution(parameterSharedState);
              var priorStates = _moduleState.PriorStates + priorState;
              _moduleState.PriorStates = priorStates
                .OrderBy(ps => ps.Name.ToUpperInvariant())
                .ToArr();
            }
          }
        }

        if (applyType.IncludesOutputs())
        {
          if (applyType.IsSet())
          {
            var paired = _moduleState.OutputStates.Map(
              os => (OS: os, ESS: elementSharedStates.Find(ess => ess.Name == os.Name))
              );

            var updated = paired.Map(p => p.ESS.Match(
              _ => p.OS.WithIsSelected(true),
              () => p.OS.WithIsSelected(false)
              ));

            var added = elementSharedStates
              .Filter(ess => !_moduleState.OutputStates.Exists(os => os.Name == ess.Name))
              .Map(ess => OutputState.Create(ess.Name));

            _moduleState.OutputStates = (updated + added)
              .OrderBy(os => os.Name)
              .ToArr();
          }
          else if (applyType.IsSingle())
          {
            var name = elementSharedStates.Head().Name;

            var index = _moduleState.OutputStates.FindIndex(os => os.Name == name);

            if (index.IsFound())
            {
              var outputState = _moduleState.OutputStates[index];
              if (!outputState.IsSelected)
              {
                outputState = outputState.WithIsSelected(true);
                _moduleState.OutputStates = _moduleState.OutputStates.SetItem(index, outputState);
              }
            }
            else
            {
              _moduleState.OutputStates = (_moduleState.OutputStates + OutputState.Create(name))
                .OrderBy(os => os.Name)
                .ToArr();
            }
          }
        }

        if (applyType.IncludesObservations())
        {
          if (applyType.IsSet())
          {
            _moduleState.SelectedObservations = observationsSharedStates;
          }
          else if (applyType.IsSingle())
          {
            if (!_moduleState.SelectedObservations.Contains(observationsSharedStates.Head()))
            {
              _moduleState.SelectedObservations += observationsSharedStates.Head();
            }
          }
        }
      }
    }

    public void ShareState(ISimSharedStateBuilder sharedStateBuilder)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        if (sharedStateBuilder.BuildType.IncludesParameters())
        {
          void ShareParameterState(ParameterState parameterState)
          {
            var (value, minimum, maximum) = _sharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
              parameterState.Name,
              _simulation.SimConfig.SimInput.SimParameters
              );

            var distribution = parameterState.GetDistribution();

            if (distribution.DistributionType == DistributionType.Invariant)
            {
              var invariantDistribution = RequireInstanceOf<InvariantDistribution>(distribution);
              value = invariantDistribution.Value;
              if (minimum > value) minimum = value.GetPreviousOrderOfMagnitude();
              if (maximum < value) maximum = value.GetNextOrderOfMagnitude();
              sharedStateBuilder.AddParameter(parameterState.Name, value, minimum, maximum, None);
            }
            else
            {
              sharedStateBuilder.AddParameter(parameterState.Name, value, minimum, maximum, Some(distribution));
            }
          }

          _moduleState.PriorStates
            .Filter(ps => ps.IsSelected)
            .Iter(ShareParameterState);
        }

        if (sharedStateBuilder.BuildType.IncludesOutputs())
        {
          _moduleState.OutputStates
            .Filter(os => os.IsSelected)
            .Map(os => os.Name)
            .Iter(sharedStateBuilder.AddOutput);
        }

        if (sharedStateBuilder.BuildType.IncludesObservations())
        {
          _moduleState.SelectedObservations
            .Map(o => _evidence.GetReference(o))
            .Iter(sharedStateBuilder.AddObservations);
        }
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
      if (_moduleState.EstimationDesign == default)
      {
        throw new InvalidOperationException("Load an estimation design");
      }

      if (_moduleState.ChainStates.IsEmpty)
      {
        throw new InvalidOperationException("No chain data");
      }

      if(!(_moduleState.PosteriorState?.BeginIteration > 0))
      {
        throw new InvalidOperationException("Convergence not set");
      }

      var title = $"Export from {nameof(Estimation)}: {_moduleState.EstimationDesign.CreatedOn.ToDirectoryName()}";

      if (_moduleState.RootExportDirectory.IsAString())
      {
        rootExportDirectory = _moduleState.RootExportDirectory;
      }
      else
      {
        rootExportDirectory = Combine(
          rootExportDirectory,
          nameof(Estimation).ToLowerInvariant(),
          _simulation.SimConfig.Title
          );
      }

      var exportDirectoryName = _moduleState.EstimationDesign.CreatedOn.ToDirectoryName();

      var openAfterExport = _moduleState.OpenAfterExport;

      var outputs = _moduleState.EstimationDesign.Outputs.Map(e => (e.Name, true));

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
      RequireNotNull(_moduleState.EstimationDesign);
      RequireNotNull(_moduleState.PosteriorState);

      var targetDirectory = Combine(
        dataExportConfiguration.RootExportDirectory,
        dataExportConfiguration.ExportDirectoryName
        );

      if (!Directory.Exists(targetDirectory))
      {
        Directory.CreateDirectory(targetDirectory);
      }

      var exportToTargetDirectory =
        par<string, Arr<string>, Simulation, ChainState>(
          ChainState.ExportChainState,
          targetDirectory,
          dataExportConfiguration.Outputs
            .Filter(o => o.IncludeInExport)
            .Map(o => o.Name),
          _simulation
        );
      _moduleState.ChainStates.Iter(exportToTargetDirectory);

      var chainNos = Range(1, _moduleState.EstimationDesign.Chains)
        .Map(i => $"\"{i}\"")
        .ToArr();

      var parameterNames = _moduleState.EstimationDesign.Priors
        .Map(mp => $"\"{mp.Name}\"")
        .ToArr();

      var outputNames = _moduleState.EstimationDesign.Outputs
        .Map(mo => $"\"{mo.Name}\"")
        .ToArr();

      var convergenceBegin = _moduleState.PosteriorState.BeginIteration;

      var r = Format(
        InvariantCulture,
        FMT_LOAD_DATA,
        targetDirectory.Replace("\\", "/"),
        Join(", ", chainNos),
        Join(", ", parameterNames),
        Join(", ", outputNames),
        convergenceBegin
        ); ;

      File.WriteAllText(Combine(targetDirectory, "load_data.R"), r);

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
          _fitViewModel.Dispose();
          _posteriorViewModel.Dispose();
          _simulationViewModel.Dispose();
          _designViewModel.Dispose();
          _likelihoodViewModel.Dispose();
          _priorsViewModel.Dispose();
          _moduleState.Dispose();

          ModuleState.Save(_moduleState, _simulation, _evidence);
        }
        _disposed = true;
      }
    }

    private void ObservePriorStateChange((Arr<ParameterState> PriorStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoShareParameterSharedState.IsTrue()) return;

      if (change.ObservableQualifier.IsAddOrChange())
      {
        var states = change.PriorStates
          .Filter(ps => ps.IsSelected)
          .Map(ps =>
          {
            var (value, minimum, maximum) = _sharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
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

        _sharedState.ShareParameterState(states);
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        _sharedState.UnshareParameterState(change.PriorStates.Map(ps => ps.Name));
      }
    }

    private void ObserveParameterSharedStateChange((Arr<SimParameterSharedState> ParameterSharedStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoApplyParameterSharedState.IsTrue()) return;

      if (change.ObservableQualifier.IsAddOrChange())
      {
        var notPartOfChange = _moduleState.PriorStates.Filter(
          ps => !change.ParameterSharedStates.Exists(pss => pss.Name == ps.Name)
          );

        var changes = change.ParameterSharedStates
          .Map(pss => _moduleState.PriorStates
            .Find(ps => ps.Name == pss.Name)
            .Match(
              ps => ApplyDistributionAndSelect(ps, pss.Value, pss.Minimum, pss.Maximum, pss.Distribution),
              () => CreateAndApplyDistribution(pss)
            )
          );

        var priorStates = notPartOfChange + changes;

        RequireUniqueElements(priorStates, ps => ps.Name);

        _moduleState.PriorStates = priorStates
          .OrderBy(ps => ps.Name.ToUpperInvariant())
          .ToArr();
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        var remaining = _moduleState.PriorStates.Filter(
          ps => !change.ParameterSharedStates.Exists(pss => pss.Name == ps.Name)
          );

        if (remaining.Count < _moduleState.PriorStates.Count)
        {
          _moduleState.PriorStates = remaining;
        }
      }
    }

    private void ObserveOutputStateChange((Arr<OutputState> OutputStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoShareElementSharedState.IsTrue()) return;

      var names = change.OutputStates.Map(os => os.Name);

      if (change.ObservableQualifier.IsAdd())
      {
        _sharedState.ShareElementState(names);
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        _sharedState.UnshareElementState(names);
      }
    }

    private void ObserveElementSharedStateChange((Arr<SimElementSharedState> ElementSharedStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoApplyElementSharedState.IsTrue()) return;

      if (change.ObservableQualifier.IsAdd())
      {
        var namesToAdd = change.ElementSharedStates
          .Map(ess => ess.Name)
          .Filter(n => !_moduleState.OutputStates.Exists(os => os.Name == n));

        var statesToAdd = namesToAdd.Map(n => OutputState.Create(n));

        _moduleState.OutputStates += statesToAdd;
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        var statesToKeep = _moduleState.OutputStates.Filter(os => !change.ElementSharedStates.Exists(ess => ess.Name == os.Name));

        _moduleState.OutputStates = statesToKeep;
      }
    }

    private void ObserveModuleStateObservationsChange((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      if (_moduleState.AutoShareObservationsSharedState.IsTrue())
      {
        var references = change.Observations.Map(o => _evidence.GetReference(o));

        if (change.ObservableQualifier.IsAdd())
        {
          _appState.SimSharedState.ShareObservationsState(references);
        }
        else if (change.ObservableQualifier.IsRemove())
        {
          _appState.SimSharedState.UnshareObservationsState(references);
        }
      }
    }

    private void ObserveSharedStateObservationsChange((Arr<SimObservationsSharedState> ObservationsSharedStates, ObservableQualifier ObservableQualifier) change)
    {
      if (_moduleState.AutoApplyObservationsSharedState.IsTrue())
      {
        var observations = change.ObservationsSharedStates
          .Map(oss => _evidence.GetObservations(oss.Reference))
          .Somes()
          .ToArr();

        if (change.ObservableQualifier.IsAdd())
        {
          _moduleState.SelectObservations(observations);
        }
        else if (change.ObservableQualifier.IsRemove())
        {
          _moduleState.UnselectObservations(observations);
        }
      }
    }

    private void ObserveEvidenceObservationsChange((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier != ObservableQualifier.Remove) return;

      var withoutRemoved = _moduleState.SelectedObservations.Filter(o => !change.Observations.Contains(o));
      if (withoutRemoved.Count < _moduleState.SelectedObservations.Count)
      {
        _moduleState.SelectedObservations = withoutRemoved;
      }
    }

    private void ObserveTargetEstimationDesignCreatedOn(object _) =>
      _designDigestsViewModel.TargetEstimationDesign.IfSome(t => LoadEstimationDesign(t.CreatedOn));

    private void ObserveModuleStateEstimationDesign(object _)
    {
      SetActivities();
    }

    private void ObserveEstimationDesignChange((DesignDigest DesignDigest, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier.IsRemove())
      {
        if (change.DesignDigest.CreatedOn == _moduleState.EstimationDesign?.CreatedOn)
        {
          _moduleState.PosteriorState = default;
          _moduleState.ChainStates = default;
          _moduleState.EstimationDesign = default;
          SetActivities();
        }
      }
      else if (change.ObservableQualifier == ObservableQualifier.Change)
      {
        _moduleState.EstimationDesign = _estimationDesigns.Load(change.DesignDigest.CreatedOn);
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
        true
        );

      return parameterState;
    }

    private void LoadEstimationDesign(DateTime createdOn)
    {
      try
      {
        var estimationDesign = _estimationDesigns.Load(createdOn);

        var existingPriorStates = _moduleState.PriorStates.Map(
          ps => estimationDesign.Priors
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

        var newPriorStates = estimationDesign.Priors
          .Filter(dp => !existingPriorStates.Exists(ps => ps.Name == dp.Name))
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

        _moduleState.PriorStates = existingPriorStates + newPriorStates;

        _moduleState.PosteriorState = default;
        _moduleState.ChainStates = default;
        _moduleState.EstimationDesign = estimationDesign;

        var pathToEstimationDesign = _estimationDesigns.GetPathToEstimationDesign(estimationDesign);
        _moduleState.ChainStates = ChainState.Load(pathToEstimationDesign);
        _moduleState.PosteriorState = PosteriorState.Load(pathToEstimationDesign);

        SetActivities();

        _designViewModel.IsSelected = true;
      }
      catch (Exception ex)
      {
        _appService.Notify(
          nameof(ViewModel),
          nameof(LoadEstimationDesign),
          ex
          );
        Log.Error(ex);
      }
    }

    private void SetActivities()
    {
      var isInDesignMode = _moduleState.EstimationDesign == default;

      _priorsViewModel.IsVisible = isInDesignMode;
      _likelihoodViewModel.IsVisible = isInDesignMode;
      _simulationViewModel.IsVisible = !isInDesignMode;
      _posteriorViewModel.IsVisible = !isInDesignMode;
      _fitViewModel.IsVisible = !isInDesignMode;
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly ISimSharedState _sharedState;
    private readonly Simulation _simulation;
    private readonly ISimEvidence _evidence;
    private readonly EstimationDesigns _estimationDesigns;
    private readonly ModuleState _moduleState;
    private readonly PriorsViewModel _priorsViewModel;
    private readonly LikelihoodViewModel _likelihoodViewModel;
    private readonly DesignViewModel _designViewModel;
    private readonly SimulationViewModel _simulationViewModel;
    private readonly PosteriorViewModel _posteriorViewModel;
    private readonly FitViewModel _fitViewModel;
    private readonly DesignDigestsViewModel _designDigestsViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
