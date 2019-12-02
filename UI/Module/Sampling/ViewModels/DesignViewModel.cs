using CsvHelper;
using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static RVis.Data.FxData;
using static Sampling.Properties.Resources;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using static System.String;
using DataTable = System.Data.DataTable;

namespace Sampling
{
  internal sealed class DesignViewModel : ViewModelBase, IDesignViewModel
  {
    internal DesignViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      ModuleState moduleState,
      SamplingDesigns samplingDesigns
      )
    {
      _appService = appService;
      _appSettings = appSettings;
      _moduleState = moduleState;
      _samplingDesigns = samplingDesigns;
      _simulation = appState.Target.AssertSome();
      _simData = appState.SimData;

      _noDesignActivityViewModel = new NoDesignActivityViewModel();
      _samplesDesignActivityViewModel = new SamplesDesignActivityViewModel();
      _outputsDesignActivityViewModel = new OutputsDesignActivityViewModel(
        appService,
        appSettings,
        _simulation.SimConfig.SimOutput,
        moduleState
        );

      GenerateSamples = ReactiveCommand.Create(
        HandleGenerateSamplesAsync,
        this.WhenAny(vm => vm.CanGenerateSamples, _ => CanGenerateSamples)
        );

      CreateDesign = ReactiveCommand.Create(
        HandleCreateDesign,
        this.WhenAny(vm => vm.CanCreateDesign, _ => CanCreateDesign)
        );

      AcquireOutputs = ReactiveCommand.Create(
        HandleAcquireOutputs,
        this.WhenAny(vm => vm.CanAcquireOutputs, _ => CanAcquireOutputs)
        );

      CancelAcquireOutputs = ReactiveCommand.Create(
        HandleCancelAcquireOutputs,
        this.WhenAny(vm => vm.CanCancelAcquireOutputs, _ => CanCancelAcquireOutputs)
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _simData.OutputRequests
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(ObserveOutputRequest),

        moduleState.ParameterStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<ParameterState>, ObservableQualifier)>(
            ObserveParameterStateChange
            )
          ),

        _moduleState
          .ObservableForProperty(ms => ms.LatinHypercubeDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateLatinHypercubeDesign
              )
            ),

        moduleState
          .ObservableForProperty(vm => vm.SamplingDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSamplingDesign
              )
            ),

        appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string>(ObserveAppSettingsPropertyChange)
            ),

        Observable
          .Merge(
            this.ObservableForProperty(vm => vm.NSamples),
            this.ObservableForProperty(vm => vm.Seed)
            )
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveInputs
              )
            )

        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        if (_moduleState.SamplingDesign == default)
        {
          Populate(_moduleState.DesignState, _moduleState.ParameterStates);
        }
        else
        {
          Populate(_moduleState.SamplingDesign);
        }

        UpdateEnable();
      }
    }

    public int? NSamples
    {
      get => _nSamples;
      set => this.RaiseAndSetIfChanged(ref _nSamples, value);
    }
    private int? _nSamples;

    public int? Seed
    {
      get => _seed;
      set => this.RaiseAndSetIfChanged(ref _seed, value);
    }
    private int? _seed;

    public ICommand GenerateSamples { get; }

    public bool CanGenerateSamples
    {
      get => _canGenerateSamples;
      set => this.RaiseAndSetIfChanged(ref _canGenerateSamples, value);
    }
    private bool _canGenerateSamples;

    public IDesignActivityViewModel ActivityViewModel
    {
      get => _activityViewModel;
      set => this.RaiseAndSetIfChanged(ref _activityViewModel, value);
    }
    private IDesignActivityViewModel _activityViewModel;

    public DateTime? CreatedOn
    {
      get => _createdOn;
      set => this.RaiseAndSetIfChanged(ref _createdOn, value);
    }
    private DateTime? _createdOn;

    public string Invariants
    {
      get => _invariants;
      set => this.RaiseAndSetIfChanged(ref _invariants, value);
    }
    private string _invariants;

    public ObservableCollection<IParameterSamplingViewModel> ParameterSamplingViewModels { get; }
      = new ObservableCollection<IParameterSamplingViewModel>();

    public ICommand CreateDesign { get; }

    public bool CanCreateDesign
    {
      get => _canCreateDesign;
      set => this.RaiseAndSetIfChanged(ref _canCreateDesign, value);
    }
    private bool _canCreateDesign;

    public ICommand AcquireOutputs { get; }

    public bool CanAcquireOutputs
    {
      get => _canAcquireOutputs;
      set => this.RaiseAndSetIfChanged(ref _canAcquireOutputs, value);
    }
    private bool _canAcquireOutputs;

    public TimeSpan? EstimatedAcquireDuration
    {
      get => _estimatedAcquireDuration;
      set => this.RaiseAndSetIfChanged(ref _estimatedAcquireDuration, value);
    }
    private TimeSpan? _estimatedAcquireDuration;

    public Arr<(int Index, NumDataTable Output)> Outputs
    {
      get => _outputs;
      set => this.RaiseAndSetIfChanged(ref _outputs, value);
    }
    private Arr<(int Index, NumDataTable Output)> _outputs;

    public ICommand CancelAcquireOutputs { get; }

    public bool CanCancelAcquireOutputs
    {
      get => _canCancelAcquireOutputs;
      set => this.RaiseAndSetIfChanged(ref _canCancelAcquireOutputs, value);
    }
    private bool _canCancelAcquireOutputs;

    public double Progress
    {
      get => _progress;
      set => this.RaiseAndSetIfChanged(ref _progress, value);
    }
    private double _progress;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
    private bool _isSelected;

    internal void ExportData(string targetDirectory, Arr<string> targetOutputs)
    {
      RequireNotNull(_moduleState.SamplingDesign);
      RequireFalse(Outputs.IsEmpty);
      RequireEqual(_moduleState.SamplingDesign.Samples.Rows.Count, Outputs.Count);
      RequireDirectory(targetDirectory);

      const string SAMPLES_FILE_NAME = "samples";
      RequireFalse(targetOutputs.Contains(SAMPLES_FILE_NAME));
      SaveToCSV<double>(_moduleState.SamplingDesign.Samples, Combine(targetDirectory, $"{SAMPLES_FILE_NAME}.csv"));

      var independentVariable = _simulation.SimConfig.SimOutput.IndependentVariable;
      var (_, output) = Outputs.Head();
      var independentData = _simulation.SimConfig.SimOutput.GetIndependentData(output);

      void SaveOutputToCSV(string name)
      {
        var pathToCSV = Combine(targetDirectory, $"{name.ToValidFileName()}.csv");

        using var streamWriter = new StreamWriter(pathToCSV);
        using var csvWriter = new CsvWriter(streamWriter);

        csvWriter.WriteField(independentData.Name);

        Range(1, Outputs.Count).Iter(i => csvWriter.WriteField($"Spl #{i}"));

        csvWriter.NextRecord();

        for (var i = 0; i < independentData.Length; ++i)
        {
          csvWriter.WriteField(independentData[i]);

          Outputs.Iter(o =>
          {
            var dependentData = o.Output[name];
            csvWriter.WriteField(dependentData[i]);
          });

          csvWriter.NextRecord();
        }
      }

      targetOutputs.Iter(SaveOutputToCSV);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        _subscriptions.Dispose();
        _outputsDesignActivityViewModel.Dispose();
      }

      base.Dispose(disposing);
    }

    private static DataTable GenerateDistributionSamples(
      Arr<ParameterState> parameterStates,
      IReadOnlyList<IParameterSamplingViewModel> parameterSamplingViewModels,
      int nSamples,
      int? seed
      )
    {
      RandomNumberGenerator.ResetGenerator(seed);

      var parameterSamples = parameterStates
        .Filter(ps => ps.IsSelected)
        .OrderBy(ps => ps.Name.ToUpperInvariant())
        .Select(ps =>
        {
          var samples = new double[nSamples];
          return (ParameterState: ps, Samples: samples);
        })
        .ToArr();

      var dataTable = new DataTable();

      parameterSamples.Iter(t =>
      {
        var distribution = t.ParameterState.GetDistribution();
        RequireTrue(distribution.IsConfigured);
        distribution.FillSamples(t.Samples);

        dataTable.Columns.Add(
          new DataColumn(
            t.ParameterState.Name,
            typeof(double)
            )
          );

        if (distribution.DistributionType != DistributionType.Invariant)
        {
          var parameterSamplingViewModel = parameterSamplingViewModels
            .Find(psvm => psvm.Parameter.Name == t.ParameterState.Name)
            .AssertSome();
          RequireTrue(parameterSamplingViewModel.Distribution.DistributionType == distribution.DistributionType);
          for (var i = 0; i < t.Samples.Length; ++i)
          {
            t.Samples[i] = t.Samples[i].ToSigFigs(5);
          }
          parameterSamplingViewModel.Samples = t.Samples.ToArr();
        }
      });

      var samplesSet = parameterSamples.Map(t => t.Samples);

      for (var row = 0; row < nSamples; ++row)
      {
        var dataRow = dataTable.NewRow();

        for (var column = 0; column < dataTable.Columns.Count; ++column)
        {
          dataRow[column] = samplesSet[column][row];
        }

        dataTable.Rows.Add(dataRow);
      }

      dataTable.AcceptChanges();

      return dataTable;
    }

    private static DataTable GenerateHypercubeSamples(
      Arr<ParameterState> parameterStates,
      LatinHypercubeDesign latinHypercubeDesign,
      IReadOnlyList<IParameterSamplingViewModel> parameterSamplingViewModels,
      int nSamples,
      int? seed,
      IRVisClient client
    )
    {
      var selectedParameters = parameterStates.Filter(ps => ps.IsSelected);

      RequireTrue(selectedParameters.ForAll(
        ps => ps.DistributionType == DistributionType.Uniform ||
              ps.DistributionType == DistributionType.Invariant
        )
      );

      var parameterBounds = selectedParameters
        .Filter(ps => ps.DistributionType == DistributionType.Uniform)
        .Map(ps =>
        {
          var uniformDistribution = RequireInstanceOf<UniformDistribution>(
            ps.GetDistribution(DistributionType.Uniform)
            );

          return (ps.Name, uniformDistribution.Lower, uniformDistribution.Upper);
        });

      var n = nSamples;
      var dimension = parameterBounds.Count;
      var randomized =
        latinHypercubeDesign.LatinHypercubeDesignType == LatinHypercubeDesignType.Randomized
          ? "TRUE"
          : "FALSE";
      var seedValue = seed?.ToString(InvariantCulture) ?? "NULL";

      var code = Format(
        InvariantCulture,
        FMT_LHSDESIGN,
        n,
        dimension,
        randomized,
        seedValue
        );

      client.EvaluateNonQuery(code);

      var useSimulatedAnnealing = !IsNaN(latinHypercubeDesign.T0);

      NumDataColumn[] design;

      if (useSimulatedAnnealing)
      {
        var t0 = latinHypercubeDesign.T0;
        var c = latinHypercubeDesign.C;
        var it = latinHypercubeDesign.Iterations;
        var p = latinHypercubeDesign.P;
        var profile = latinHypercubeDesign.Profile switch
        {
          TemperatureDownProfile.Geometrical => "GEOM",
          TemperatureDownProfile.GeometricalMorris => "GEOM_MORRIS",
          TemperatureDownProfile.Linear => "LINEAR",
          _ => throw new ArgumentOutOfRangeException(nameof(TemperatureDownProfile))
        };
        var imax = latinHypercubeDesign.Imax;

        code = Format(
          InvariantCulture,
          FMT_MAXIMINSALHS,
          t0,
          c,
          it,
          p,
          profile,
          imax
          );

        client.EvaluateNonQuery(code);
        design = client.EvaluateNumData("rvis_lhsDesign_out_opt$design");
      }
      else
      {
        design = client.EvaluateNumData("rvis_lhsDesign_out$design");
      }

      var dataTable = new DataTable();

      selectedParameters.Iter(pb =>
      {
        dataTable.Columns.Add(
          new DataColumn(
            pb.Name,
            typeof(double)
            )
          );
      });

      var itemArray = Enumerable
        .Repeat(NaN, selectedParameters.Count)
        .Cast<object>()
        .ToArray();

      selectedParameters.Iter((i, ps) =>
      {
        if (ps.DistributionType != DistributionType.Invariant) return;
        var distribution = RequireInstanceOf<InvariantDistribution>(ps.GetDistribution());
        itemArray[i] = distribution.Value;
      });

      var targetIndices = itemArray
        .Cast<double>()
        .Select((d, i) => IsNaN(d) ? i : NOT_FOUND)
        .Where(i => i.IsFound())
        .ToArr();

      RequireTrue(targetIndices.Count == parameterBounds.Count);

      Range(0, nSamples).Iter(i =>
      {
        targetIndices.Iter((j, ti) =>
        {
          var value = design[j][i];
          var (_, lower, upper) = parameterBounds[j];
          value = lower + value * (upper - lower);
          itemArray[ti] = value.ToSigFigs(5);
        });

        dataTable.Rows.Add(itemArray);
      });

      dataTable.AcceptChanges();

      parameterSamplingViewModels.Iter(
        vm => vm.Samples = Range(0, nSamples)
          .Map(i => dataTable.Rows[i].Field<double>(vm.Parameter.Name))
          .ToArr()
        );

      return dataTable;
    }

    private async Task HandleGenerateSamplesAsync()
    {
      RequireTrue(NSamples > 0);

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        UnloadCurrentDesign();
        _moduleState.SamplingDesign = default;

        DataTable samples = default;

        if (_moduleState.LatinHypercubeDesign.LatinHypercubeDesignType == LatinHypercubeDesignType.None)
        {
          TaskName = "Generating Samples";
          IsRunningTask = true;

          try
          {
            samples = await Task.Run(() => GenerateDistributionSamples(
              _moduleState.ParameterStates,
              ParameterSamplingViewModels,
              NSamples.Value,
              Seed
            ));
          }
          catch (Exception ex)
          {
            _appService.Notify(
              NotificationType.Error,
              nameof(DesignViewModel),
              nameof(GenerateDistributionSamples),
              ex.Message
              );
          }

          IsRunningTask = false;
        }
        else
        {
          async Task<DataTable> SomeServer(ServerLicense serverLicense)
          {
            using (serverLicense)
            {
              return await Task.Run(() => GenerateHypercubeSamples(
                _moduleState.ParameterStates,
                _moduleState.LatinHypercubeDesign,
                ParameterSamplingViewModels,
                NSamples.Value,
                Seed,
                serverLicense.Client
              ));
            }
          }

          Task<DataTable> NoServer()
          {
            _appService.Notify(
              NotificationType.Information,
              nameof(DesignViewModel),
              nameof(GenerateHypercubeSamples),
              "No R server available."
              );

            return default;
          }

          TaskName = "Generating Hypercube Samples";
          IsRunningTask = true;

          try
          {
            samples = await _appService.RVisServerPool
              .RequestServer()
              .MatchUnsafeAsync(SomeServer, NoServer);
          }
          catch (Exception ex)
          {
            _appService.Notify(
              NotificationType.Error,
              nameof(DesignViewModel),
              nameof(GenerateHypercubeSamples),
              ex.Message
              );
          }

          IsRunningTask = false;

          if (samples == default) return;
        }

        CreatedOn = DateTime.Now;

        var (ms, _) = _simData.GetExecutionInterval(_simulation).IfNone((0, 0));
        var msExecutionTime = ms * samples.Rows.Count / _appSettings.RThrottlingUseCores;
        EstimatedAcquireDuration = TimeSpan.FromMilliseconds(msExecutionTime);

        _samplesDesignActivityViewModel.Samples = samples.DefaultView;
        ActivityViewModel = _samplesDesignActivityViewModel;

        UpdateEnable();
      }
    }

    private void HandleCreateDesign()
    {
      RequireTrue(CreatedOn.HasValue);
      RequireFalse(_samplesDesignActivityViewModel.Samples == default);

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        var parameterDistributions = _moduleState.ParameterStates
          .Filter(ps => ps.IsSelected)
          .OrderBy(ps => ps.Name.ToUpperInvariant())
          .Select(ps => (ps.Name, Distribution: ps.GetDistribution()))
          .ToArr();

        RequireTrue(parameterDistributions.ForAll(t => t.Distribution.IsConfigured));

        var samplingDesign = SamplingDesign.CreateSamplingDesign(
          CreatedOn.Value,
          parameterDistributions,
          Seed,
          _samplesDesignActivityViewModel.Samples.Table,
          default
          );

        _samplingDesigns.Add(samplingDesign);
        _moduleState.SamplingDesign = samplingDesign;

        Invariants = GetInvariants(samplingDesign.DesignParameters);

        _outputRequestJob = CompileOutputRequestJob(
          _simulation,
          _simData,
          samplingDesign.Samples,
          samplingDesign.NoDataIndices
          );

        var nSimRuns = _outputRequestJob.Count(t =>
        {
          if (t.Input == default) return false;
          if (t.Output != default) return false;
          if (_simulation.HasData(t.Input)) return false;

          return true;
        });

        EstimatedAcquireDuration = GetEstimatedAcquireDuration(nSimRuns);

        _outputsDesignActivityViewModel.Initialize(_outputRequestJob.Length);
        ActivityViewModel = _outputsDesignActivityViewModel;

        UpdateEnable();
      }
    }

    private void HandleAcquireOutputs()
    {
      RequireNotNull(_outputRequestJob);
      RequireFalse(_runOutputRequestJob);
      RequireTrue(Outputs.IsEmpty);

      _runOutputRequestJob = true;

      var nConcurrent = _appSettings.RThrottlingUseCores;

      for (var i = 0; i < _outputRequestJob.Length; ++i)
      {
        var (input, _, output) = _outputRequestJob[i];

        if (input == default || output != default) continue;

        _simData.RequestOutput(_simulation, input, this, i, true);

        _outputRequestJob[i] = (input, true, default);

        if (--nConcurrent == 0) break;
      }

      Progress = 0.0;

      UpdateEnable();
    }

    private void HandleCancelAcquireOutputs()
    {
      RequireTrue(_runOutputRequestJob);

      _runOutputRequestJob = false;

      for (var i = 0; i < _outputRequestJob.Length; ++i)
      {
        var (input, _, output) = _outputRequestJob[i];
        _outputRequestJob[i] = (input, false, output);
      }

      UpdateEnable();
    }

    private void ObserveOutputRequest(SimDataItem<OutputRequest> outputRequest)
    {
      if (outputRequest.Requester != this) return;
      if (_outputRequestJob == default) return;

      RequireTrue(outputRequest.RequestToken.Resolve(out int thisIndex));
      if (thisIndex >= _outputRequestJob.Length) return;

      var (input, outputRequested, output) = _outputRequestJob[thisIndex];
      if (input.Hash != outputRequest.Item.SeriesInput.Hash) return;

      outputRequest.Item.SerieInputs.Match(
        si =>
        {
          RequireTrue(si.Count == 1);
          output = _simData.GetOutput(si[0], _simulation).AssertSome();
        },
        _ => input = default
        );

      _outputRequestJob[thisIndex] = (input, true, output);

      if (output != default)
      {
        _outputsDesignActivityViewModel.AddOutput(thisIndex, output);
      }

      if (_runOutputRequestJob)
      {
        var nextIndex = _outputRequestJob.FindIndex(
          t => t.Input != default && !t.OutputRequested && t.Output == default
          );

        if (nextIndex.IsFound())
        {
          (input, _, _) = _outputRequestJob[nextIndex];
          _outputRequestJob[nextIndex] = (input, true, default);
          _appService.ScheduleLowPriorityAction(
            () => _simData.RequestOutput(_simulation, input, this, nextIndex, true)
          );
        }
      }

      UpdateOutputRequestProgress();
    }

    private void UpdateOutputRequestProgress()
    {
      var nCompleted = _outputRequestJob.Count(t => t.Input == default || t.Output != default);
      Progress = 100.0 * nCompleted / _outputRequestJob.Length;

      if (nCompleted < _outputRequestJob.Length) return;

      _runOutputRequestJob = false;

      if (_moduleState.SamplingDesign.NoDataIndices.IsEmpty)
      {
        var noDataIndices = _outputRequestJob
          .Select((t, i) => t.Output == default ? Some(i) : None)
          .Somes()
          .ToArr();

        if (!noDataIndices.IsEmpty)
        {
          var samplingDesign = new SamplingDesign(
            _moduleState.SamplingDesign.CreatedOn,
            _moduleState.SamplingDesign.DesignParameters,
            _moduleState.SamplingDesign.Seed,
            _moduleState.SamplingDesign.Samples,
            noDataIndices
            );

          using (_reactiveSafeInvoke.SuspendedReactivity)
          {
            _samplingDesigns.Update(samplingDesign);
          }
        }
      }

      Outputs = _outputRequestJob
        .Select((t, i) => (Index: i, t.Output))
        .Filter(t => t.Output != default)
        .ToArr();

      _outputRequestJob = default;
      EstimatedAcquireDuration = default;

      UpdateEnable();
    }

    private void ObserveParameterStateChange((Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier) change)
    {
      UnloadCurrentDesign();
      _moduleState.SamplingDesign = default;

      if (change.ObservableQualifier.IsAddOrChange())
      {
        var paired = change.ParameterStates
          .Map(ps =>
          {
            var vm = ParameterSamplingViewModels.SingleOrDefault(psvm => psvm.Parameter.Name == ps.Name);
            return (PS: ps, Dist: ps.GetDistribution(), VM: vm);
          })
          .Filter(t => t.Dist.DistributionType != DistributionType.Invariant);

        paired.Filter(t => t.VM == default && t.PS.IsSelected).Iter(t =>
        {
          var parameter = _simulation.SimConfig.SimInput.SimParameters.GetParameter(t.PS.Name);
          var parameterSamplingViewModel = new ParameterSamplingViewModel(parameter)
          {
            Distribution = t.Dist
          };
          parameterSamplingViewModel.Histogram.ApplyThemeToPlotModelAndAxes();
          ParameterSamplingViewModels.InsertInOrdered(parameterSamplingViewModel, vm => vm.SortKey);
        });

        paired
          .Filter(t => t.VM != default && t.PS.IsSelected)
          .Iter(t => t.VM.Distribution = t.Dist);

        paired
          .Filter(t => t.VM != default && !t.PS.IsSelected)
          .Iter(t => ParameterSamplingViewModels.Remove(t.VM));
      }
      else if (change.ObservableQualifier.IsRemove())
      {
        change.ParameterStates.Iter(
          ps => ParameterSamplingViewModels.RemoveIf(
            psvm => psvm.Parameter.Name == ps.Name
            )
          );
      }

      Invariants = GetInvariants(_moduleState.ParameterStates);

      ActivityViewModel = _noDesignActivityViewModel;

      UpdateEnable();
    }

    private void ObserveModuleStateLatinHypercubeDesign(object _)
    {
      UnloadCurrentDesign();
      _moduleState.SamplingDesign = default;
      ActivityViewModel = _noDesignActivityViewModel;
      UpdateEnable();
    }

    private void ObserveModuleStateSamplingDesign(object _)
    {
      UnloadCurrentDesign();

      if (_moduleState.SamplingDesign == default)
      {
        Populate(_moduleState.DesignState, _moduleState.ParameterStates);
      }
      else
      {
        Populate(_moduleState.SamplingDesign);
      }

      UpdateEnable();
    }

    private void ObserveAppSettingsPropertyChange(string propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      foreach (var parameterSamplingViewModel in ParameterSamplingViewModels)
      {
        parameterSamplingViewModel.Histogram.ApplyThemeToPlotModelAndAxes();
        parameterSamplingViewModel.Histogram.InvalidatePlot(false);
      }
    }

    private void ObserveInputs(object _)
    {
      UnloadCurrentDesign();

      _moduleState.SamplingDesign = default;
      _moduleState.DesignState.NumberOfSamples = NSamples;
      _moduleState.DesignState.Seed = Seed;

      ActivityViewModel = _noDesignActivityViewModel;

      UpdateEnable();
    }

    private void UnloadCurrentDesign()
    {
      _runOutputRequestJob = false;
      _outputRequestJob = default;
      ParameterSamplingViewModels.Iter(psvm => psvm.Samples = default);
      CreatedOn = default;
      EstimatedAcquireDuration = default;
      Outputs = default;
      Progress = 0.0;

      _samplesDesignActivityViewModel.Samples = default;
      _outputsDesignActivityViewModel.Clear();
    }

    private void UpdateEnable()
    {
      CanGenerateSamples = NSamples > 0;
      CanCreateDesign = _samplesDesignActivityViewModel.Samples != default && _moduleState.SamplingDesign == default;
      CanAcquireOutputs = _outputRequestJob != default && !_runOutputRequestJob;
      CanCancelAcquireOutputs = _runOutputRequestJob;
    }

    private void Populate(DesignState designState, Arr<ParameterState> parameterStates)
    {
      NSamples = designState.NumberOfSamples ?? 100;
      Seed = designState.Seed;
      CreatedOn = default;
      Invariants = GetInvariants(parameterStates);

      var parameterSamplingViewModels = parameterStates
        .Filter(ps => ps.IsSelected && ps.DistributionType != DistributionType.Invariant)
        .Map(ps =>
        {
          var parameter = _simulation.SimConfig.SimInput.SimParameters.GetParameter(ps.Name);
          var parameterSamplingViewModel = new ParameterSamplingViewModel(parameter)
          {
            Distribution = ps.GetDistribution()
          };
          parameterSamplingViewModel.Histogram.ApplyThemeToPlotModelAndAxes();
          return parameterSamplingViewModel;
        })
        .OrderBy(psvm => psvm.SortKey)
        .ToArr();
      ParameterSamplingViewModels.Clear();
      parameterSamplingViewModels.Iter(ParameterSamplingViewModels.Add);

      _samplesDesignActivityViewModel.Samples = default;
      EstimatedAcquireDuration = default;
      Outputs = default;

      ActivityViewModel = _noDesignActivityViewModel;
    }

    private void Populate(SamplingDesign samplingDesign)
    {
      NSamples = samplingDesign.Samples.Rows.Count;
      Seed = samplingDesign.Seed;
      CreatedOn = samplingDesign.CreatedOn;
      Invariants = GetInvariants(samplingDesign.DesignParameters);

      var parameterSamplingViewModels = samplingDesign.DesignParameters
        .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
        .Map(dp =>
        {
          var parameter = _simulation.SimConfig.SimInput.SimParameters.GetParameter(dp.Name);
          var parameterSamplingViewModel = new ParameterSamplingViewModel(parameter)
          {
            Distribution = dp.Distribution
          };
          parameterSamplingViewModel.Histogram.ApplyThemeToPlotModelAndAxes();

          var columnIndex = samplingDesign.Samples.Columns.IndexOf(dp.Name);
          RequireTrue(columnIndex.IsFound());
          var samples = samplingDesign.Samples
            .AsEnumerable()
            .Select(dr => dr.Field<double>(columnIndex))
            .ToArr();
          parameterSamplingViewModel.Samples = samples;

          return parameterSamplingViewModel;
        })
        .OrderBy(psvm => psvm.SortKey)
        .ToArr();
      ParameterSamplingViewModels.Clear();
      parameterSamplingViewModels.Iter(ParameterSamplingViewModels.Add);

      _samplesDesignActivityViewModel.Samples = samplingDesign.Samples.DefaultView;

      var outputRequestJob = CompileOutputRequestJob(
        _simulation,
        _simData,
        samplingDesign.Samples,
        samplingDesign.NoDataIndices
        );

      var outputs = outputRequestJob
        .Select((t, i) => (Index: i, t.Output))
        .Where(t => t.Output != default)
        .ToArr();

      _outputsDesignActivityViewModel.Initialize(outputRequestJob.Length);
      _outputsDesignActivityViewModel.AddOutputs(outputs);

      var allOutputsPresent = outputRequestJob.All(t => t.Input == default || t.Output != default);

      if (allOutputsPresent)
      {
        _outputRequestJob = default;

        EstimatedAcquireDuration = default;

        Outputs = outputRequestJob
          .Select((t, i) => (Index: i, t.Output))
          .Where(t => t.Output != default)
          .ToArr();
      }
      else
      {
        _outputRequestJob = outputRequestJob;

        var nSimRuns = _outputRequestJob.Count(t =>
        {
          if (t.Input == default) return false;
          if (t.Output != default) return false;
          if (_simulation.HasData(t.Input)) return false;

          return true;
        });

        EstimatedAcquireDuration = GetEstimatedAcquireDuration(nSimRuns);

        Outputs = default;
      }

      ActivityViewModel = _outputsDesignActivityViewModel;
    }

    private TimeSpan GetEstimatedAcquireDuration(int nSimRuns)
    {
      var (ms, _) = _simData.GetExecutionInterval(_simulation).IfNone((0, 0));
      var msExecutionTime = ms * nSimRuns / _appSettings.RThrottlingUseCores;
      return TimeSpan.FromMilliseconds(msExecutionTime);
    }

    private static string GetInvariants(Arr<ParameterState> parameterStates) =>
      Join(", ", parameterStates
        .Filter(ps => ps.IsSelected)
        .Map(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .Filter(t => t.Distribution.DistributionType == DistributionType.Invariant)
        .Map(t => t.Distribution.ToString(t.Name))
        );

    private static string GetInvariants(Arr<DesignParameter> designParameters) =>
      Join(", ", designParameters
        .Filter(dp => dp.Distribution.DistributionType == DistributionType.Invariant)
        .Map(dp => dp.Distribution.ToString(dp.Name))
        );

    private static (SimInput Input, bool OutputRequested, NumDataTable Output)[] CompileOutputRequestJob(
      Simulation simulation,
      ISimData simData,
      DataTable samples,
      Arr<int> noDataIndices
      )
    {
      var job = new (SimInput Input, bool OutputRequested, NumDataTable Output)[samples.Rows.Count];
      var defaultInput = simulation.SimConfig.SimInput;

      var targetParameters = samples.Columns
        .Cast<DataColumn>()
        .Select(dc => defaultInput.SimParameters.GetParameter(dc.ColumnName))
        .ToArr();

      for (var row = 0; row < samples.Rows.Count; ++row)
      {
        if (noDataIndices.Contains(row)) continue;

        var dataRow = samples.Rows[row];

        var sampleParameters = targetParameters.Map(
          (i, p) => p.With(dataRow.Field<double>(i))
          );

        var input = defaultInput.With(sampleParameters.ToArr());

        var output = simData.GetOutput(input, simulation).IfNoneUnsafe(default(NumDataTable));

        job[row] = (input, false, output);
      }

      return job;
    }

    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly SamplingDesigns _samplingDesigns;
    private readonly NoDesignActivityViewModel _noDesignActivityViewModel;
    private readonly SamplesDesignActivityViewModel _samplesDesignActivityViewModel;
    private readonly OutputsDesignActivityViewModel _outputsDesignActivityViewModel;
    private readonly Simulation _simulation;
    private readonly ISimData _simData;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private (SimInput Input, bool OutputRequested, NumDataTable Output)[] _outputRequestJob;
    private bool _runOutputRequestJob;
  }
}
