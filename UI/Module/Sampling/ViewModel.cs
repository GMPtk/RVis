using CsvHelper;
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
using static RVis.Data.FxData;
using static Sampling.Logger;
using static Sampling.Properties.Resources;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using static System.String;

namespace Sampling
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

      var pathToSamplingDesignsDirectory = _simulation.GetPrivateDirectory(nameof(Sampling), nameof(SamplingDesigns));
      _samplingDesigns = new SamplingDesigns(pathToSamplingDesignsDirectory);

      _moduleState = ModuleState.LoadOrCreate(_simulation, _samplingDesigns);

      _parametersViewModel = new ParametersViewModel(appState, appService, appSettings, _moduleState);
      _samplesViewModel = new SamplesViewModel(appState, appService, appSettings, _moduleState);
      _designViewModel = new DesignViewModel(appState, appService, appSettings, _moduleState, _samplingDesigns);
      _outputsViewModel = new OutputsViewModel(appState, appService, appSettings, _moduleState);
      _designDigestsViewModel = new DesignDigestsViewModel(appService, _moduleState, _samplingDesigns);

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
          .ObservableForProperty(vm => vm.TargetSamplingDesign).Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveTargetSamplingDesign
              )
          ),

        _moduleState
          .ObservableForProperty(ms => ms.SamplingDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSamplingDesign
              )
            ),

        _samplingDesigns.SamplingDesignChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(DesignDigest, ObservableQualifier)>(
            ObserveSamplingDesignChange
            )
          )

        );

      SetActivities();

      if (_moduleState.SamplingDesign != default) _designViewModel.IsSelected = true;
    }

    public IParametersViewModel ParametersViewModel => _parametersViewModel;

    public ISamplesViewModel SamplesViewModel => _samplesViewModel;

    public IDesignViewModel DesignViewModel => _designViewModel;

    public IOutputsViewModel OutputsViewModel => _outputsViewModel;

    public IDesignDigestsViewModel DesignDigestsViewModel => _designDigestsViewModel;

    public Arr<ITaskRunner> GetTaskRunners() => Array<ITaskRunner>(_samplesViewModel);

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
      if (_moduleState.SamplingDesign == default)
      {
        throw new InvalidOperationException("Load a sampling design");
      }

      if (_moduleState.Outputs.IsEmpty)
      {
        throw new InvalidOperationException("Acquire sampling outputs");
      }

      var title = $"Export from {nameof(Sampling)}: {_moduleState.SamplingDesign.CreatedOn.ToDirectoryName()}";

      if (_moduleState.RootExportDirectory.IsAString())
      {
        rootExportDirectory = _moduleState.RootExportDirectory;
      }
      else
      {
        rootExportDirectory = Combine(
          rootExportDirectory,
          nameof(Sampling).ToLowerInvariant(),
          _simulation.SimConfig.Title
          );
      }

      var exportDirectoryName = _moduleState.SamplingDesign.CreatedOn.ToDirectoryName();

      var openAfterExport = _moduleState.OpenAfterExport;

      var outputs = _simulation.SimConfig.SimOutput.DependentVariables.Map(e => (e.Name, false));

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

      ExportData(
        targetDirectory,
        dataExportConfiguration.Outputs
          .Filter(o => o.IncludeInExport)
          .Map(o => o.Name)
        );

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
          _outputsViewModel.Dispose();
          _designViewModel.Dispose();
          _samplesViewModel.Dispose();
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
      var isUnloading = _moduleState.SamplingDesign != default;

      UnloadDesign();
      SetActivities();

      if (isUnloading) _designViewModel.IsSelected = true;

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

    private void ObserveTargetSamplingDesign(object _) =>
      _designDigestsViewModel.TargetSamplingDesign.IfSome(
        t => LoadSamplingDesign(t.CreatedOn)
        );

    private void ObserveModuleStateSamplingDesign(object _)
    {
      SetActivities();
    }

    private void ObserveSamplingDesignChange(
      (DesignDigest DesignDigest, ObservableQualifier ObservableQualifier) change
      )
    {
      if (change.ObservableQualifier.IsRemove())
      {
        if (change.DesignDigest.CreatedOn == _moduleState.SamplingDesign?.CreatedOn)
        {
          UnloadDesign();
          SetActivities();
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
          var index = parameterState.Distributions.FindIndex(
            e => e.DistributionType == d.DistributionType
            );
          var distributions = parameterState.Distributions.SetItem(index, d);
          return new ParameterState(
            parameterState.Name,
            d.DistributionType,
            distributions,
            isSelected: true
            );
        },
        () =>
        {
          var invariantDistribution = new InvariantDistribution(value);
          var distributions = parameterState.Distributions.SetDistribution(invariantDistribution);
          parameterState = new ParameterState(
            parameterState.Name,
            DistributionType.Invariant,
            distributions,
            isSelected: true
            );
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

    private void LoadSamplingDesign(DateTime createdOn)
    {
      try
      {
        var samplingDesign = _samplingDesigns.Load(createdOn);

        var existingParameterStates = _moduleState.ParameterStates.Map(
          ps => samplingDesign.DesignParameters
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

        var newParameterStates = samplingDesign.DesignParameters
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

        var parameterStates = existingParameterStates + newParameterStates;
        _moduleState.ParameterStates = parameterStates
          .OrderBy(ps => ps.Name.ToUpperInvariant())
          .ToArr();

        UnloadDesign();

        _moduleState.SamplesState.NumberOfSamples = samplingDesign.Samples.Rows.Count;
        _moduleState.SamplesState.Seed = samplingDesign.Seed;
        _moduleState.SamplesState.LatinHypercubeDesign = samplingDesign.LatinHypercubeDesign;
        _moduleState.SamplesState.RankCorrelationDesign = samplingDesign.RankCorrelationDesign;

        _moduleState.SamplingDesign = samplingDesign;
        _moduleState.Samples = samplingDesign.Samples;

        SetActivities();

        _designViewModel.IsSelected = true;
      }
      catch (Exception ex)
      {
        _appService.Notify(
          nameof(ViewModel),
          nameof(LoadSamplingDesign),
          ex
          );
        Log.Error(ex);
      }
    }

    private void UnloadDesign()
    {
      _moduleState.Outputs = default;
      _moduleState.Samples = default;
      _moduleState.SamplingDesign = default;
    }

    private void SetActivities()
    {
      var isInDesignMode = _moduleState.SamplingDesign == default;
      _parametersViewModel.IsVisible = isInDesignMode;
      _samplesViewModel.IsReadOnly = !isInDesignMode;
      _outputsViewModel.IsVisible = !isInDesignMode;
    }

    internal void ExportData(string targetDirectory, Arr<string> targetOutputs)
    {
      RequireNotNull(_moduleState.SamplingDesign);
      RequireFalse(_moduleState.Outputs.IsEmpty);
      RequireDirectory(targetDirectory);

      const string SAMPLES_FILE_NAME = "samples";
      RequireFalse(targetOutputs.Contains(SAMPLES_FILE_NAME));
      SaveToCSV<double>(
        _moduleState.SamplingDesign.Samples,
        Combine(targetDirectory, $"{SAMPLES_FILE_NAME}.csv")
        );

      var independentVariable = _simulation.SimConfig.SimOutput.IndependentVariable;
      var (_, output) = _moduleState.Outputs.Head();
      var independentData = _simulation.SimConfig.SimOutput.GetIndependentData(output);

      void SaveOutputToCSV(string name)
      {
        var pathToCSV = Combine(targetDirectory, $"{name.ToValidFileName()}.csv");

        using var streamWriter = new StreamWriter(pathToCSV);
        using var csvWriter = new CsvWriter(streamWriter);

        csvWriter.WriteField(independentData.Name);

        var nSamples = _moduleState.SamplingDesign.Samples.Rows.Count;

        Range(1, nSamples).Iter(i => csvWriter.WriteField($"Spl #{i}"));

        csvWriter.NextRecord();

        for (var i = 0; i < independentData.Length; ++i)
        {
          csvWriter.WriteField(independentData[i]);

          Range(0, nSamples).Iter(j =>
          {
            var output = _moduleState.Outputs
              .Find(o => o.Index == j)
              .Match(o => o.Output[name][i], () => NaN);
            
            csvWriter.WriteField(output);
          });

          csvWriter.NextRecord();
        }
      }

      targetOutputs.Iter(SaveOutputToCSV);

      var parameterNames = _moduleState.SamplingDesign.DesignParameters
        .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
        .Map(dp => $"\"{dp.Name}\"");

      var outputNames = targetOutputs.Map(o => $"\"{o}\"");

      var r = Format(
        InvariantCulture,
        FMT_LOAD_DATA,
        targetDirectory.Replace("\\", "/"),
        Join(", ", parameterNames),
        SAMPLES_FILE_NAME,
        Join(", ", outputNames)
        );

      File.WriteAllText(Combine(targetDirectory, "load_data.R"), r);
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly Simulation _simulation;
    private readonly SamplingDesigns _samplingDesigns;
    private readonly ModuleState _moduleState;
    private readonly ParametersViewModel _parametersViewModel;
    private readonly SamplesViewModel _samplesViewModel;
    private readonly DesignViewModel _designViewModel;
    private readonly OutputsViewModel _outputsViewModel;
    private readonly DesignDigestsViewModel _designDigestsViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
