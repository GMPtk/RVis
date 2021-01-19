using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using DataTable = System.Data.DataTable;

namespace Sampling
{
  internal sealed class DesignViewModel : IDesignViewModel, INotifyPropertyChanged, IDisposable
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

      CreateDesign = ReactiveCommand.Create(
        HandleCreateDesign,
        this.WhenAny(vm => vm.CanCreateDesign, _ => CanCreateDesign)
        );

      UnloadDesign = ReactiveCommand.Create(
        HandleUnloadDesign,
        this.WhenAny(vm => vm.CanUnloadDesign, _ => CanUnloadDesign)
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

        moduleState
          .ObservableForProperty(vm => vm.Samples)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSamples
              )
            ),

        moduleState
          .ObservableForProperty(vm => vm.SamplingDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSamplingDesign
              )
            )

        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();

        if (_moduleState.SamplingDesign != default)
        {
          ConfigureOutputsAcquisition();
        }

        UpdateEnable();
      }
    }

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public DateTime? CreatedOn
    {
      get => _createdOn;
      set => this.RaiseAndSetIfChanged(ref _createdOn, value, PropertyChanged);
    }
    private DateTime? _createdOn;

    public ICommand CreateDesign { get; }

    public bool CanCreateDesign
    {
      get => _canCreateDesign;
      set => this.RaiseAndSetIfChanged(ref _canCreateDesign, value, PropertyChanged);
    }
    private bool _canCreateDesign;

    public ICommand UnloadDesign { get; }

    public bool CanUnloadDesign
    {
      get => _canUnloadDesign;
      set => this.RaiseAndSetIfChanged(ref _canUnloadDesign, value, PropertyChanged);
    }
    private bool _canUnloadDesign;

    public double AcquireOutputsProgress
    {
      get => _acquireOutputsProgress;
      set => this.RaiseAndSetIfChanged(ref _acquireOutputsProgress, value, PropertyChanged);
    }
    private double _acquireOutputsProgress;

    public int NOutputsAcquired
    {
      get => _nOutputsAcquired;
      set => this.RaiseAndSetIfChanged(ref _nOutputsAcquired, value, PropertyChanged);
    }
    private int _nOutputsAcquired;

    public int NOutputsToAcquire
    {
      get => _nOutputsToAcquire;
      set => this.RaiseAndSetIfChanged(ref _nOutputsToAcquire, value, PropertyChanged);
    }
    private int _nOutputsToAcquire;

    public ICommand AcquireOutputs { get; }

    public bool CanAcquireOutputs
    {
      get => _canAcquireOutputs;
      set => this.RaiseAndSetIfChanged(ref _canAcquireOutputs, value, PropertyChanged);
    }
    private bool _canAcquireOutputs;

    public ICommand CancelAcquireOutputs { get; }

    public bool CanCancelAcquireOutputs
    {
      get => _canCancelAcquireOutputs;
      set => this.RaiseAndSetIfChanged(ref _canCancelAcquireOutputs, value, PropertyChanged);
    }
    private bool _canCancelAcquireOutputs;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(disposing: true);

    private void HandleCreateDesign()
    {
      RequireTrue(CreatedOn.HasValue);
      RequireFalse(_moduleState.Samples == default);
      RequireOrdered(_moduleState.ParameterStates, ps => ps.Name.ToUpperInvariant());

      var parameterDistributions = _moduleState.ParameterStates
        .Filter(ps => ps.IsSelected)
        .Select(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .ToArr();

      RequireTrue(parameterDistributions.ForAll(t => t.Distribution.IsConfigured));

      var samplingDesign = SamplingDesign.CreateSamplingDesign(
        CreatedOn.Value,
        parameterDistributions,
        _moduleState.SamplesState.LatinHypercubeDesign,
        _moduleState.SamplesState.RankCorrelationDesign,
        _moduleState.SamplesState.Seed,
        _moduleState.Samples,
        default
        );

      _samplingDesigns.Add(samplingDesign);
      _moduleState.SamplingDesign = samplingDesign;

      UpdateEnable();
    }

    private void HandleUnloadDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        UnloadCurrentDesign();
        _moduleState.Outputs = default;
        _moduleState.FilterConfig = FilterConfig.Default;
        _moduleState.OutputFilters = default;
        _moduleState.Samples = default;
        _moduleState.SamplingDesign = default;
        UpdateEnable();
      }
    }

    private void HandleAcquireOutputs()
    {
      RequireNotNull(_outputRequestJob);
      RequireFalse(_runOutputRequestJob);
      RequireTrue(NOutputsAcquired < NOutputsToAcquire);

      _jobSubscriptions = new CompositeDisposable(
        _simData.OutputRequests.Subscribe(ObserveOutputRequest),
        _appService.SecondInterval.Subscribe(ObserveSecondInterval)
        );

      _runOutputRequestJob = true;

      AcquireOutputsProgress = 100.0 * NOutputsAcquired / _outputRequestJob.Length;

      UpdateEnable();

      lock (_jobSyncLock)
      {
        var nConcurrent = _appSettings.RThrottlingUseCores;

        for (var i = 0; i < _outputRequestJob.Length; ++i)
        {
          var (input, outputRequested, output) = _outputRequestJob[i];

          var failedOrInProgressOrCompleted =
            input == default ||
            outputRequested ||
            output != default;

          if (failedOrInProgressOrCompleted) continue;

          _outputRequestJob[i] = (input, true, default);

          var didRequest = _simData.RequestOutput(
            _simulation,
            input,
            this,
            i,
            persist: true
            );

          if (!didRequest) continue;

          if (--nConcurrent == 0) break;
        }
      }
    }

    private void HandleCancelAcquireOutputs()
    {
      lock (_jobSyncLock)
      {
        if (!_runOutputRequestJob) return;

        RequireNotNull(_outputRequestJob);

        _runOutputRequestJob = false;
        _jobSubscriptions?.Dispose();
        _jobSubscriptions = default;

        for (var i = 0; i < _outputRequestJob.Length; ++i)
        {
          var (input, outputRequested, output) = _outputRequestJob[i];
          if (outputRequested)
          {
            _outputRequestJob[i] = (input, false, output);
          }
        }

        UpdateEnable();
      }
    }

    private void ObserveOutputRequest(SimDataItem<OutputRequest> outputRequest)
    {
      if (outputRequest.Requester != this) return;

      lock (_jobSyncLock)
      {
        if (!_runOutputRequestJob) return;

        RequireNotNull(_outputRequestJob);

        RequireTrue(outputRequest.RequestToken.Resolve(out int thisIndex));
        if (thisIndex >= _outputRequestJob.Length) return;

        var (thisInput, _, thisOutput) = _outputRequestJob[thisIndex];
        if (thisInput.Hash != outputRequest.Item.SeriesInput.Hash) return;
        if (thisOutput != default) return;

        void RequestSucceeded(Arr<SimInput> serieInputs)
        {
          RequireTrue(serieInputs.Count == 1);

          thisOutput = _simData
            .GetOutput(serieInputs[0], _simulation)
            .AssertSome();
        }

        void RequestFailed(Exception ex)
        {
          thisInput = default;
        }

        outputRequest.Item.SerieInputs.Match(
          RequestSucceeded,
          RequestFailed
          );

        _outputRequestJob[thisIndex] = (thisInput, false, thisOutput);

        for (var i = thisIndex + 1; i < _outputRequestJob.Length; ++i)
        {
          if (_outputRequestJob[i].Input.Hash != outputRequest.Item.SeriesInput.Hash) continue;
          _outputRequestJob[i] = (thisInput, false, thisOutput);
        }

        for (var i = 0; i < _outputRequestJob.Length; ++i)
        {
          var (input, outputRequested, output) = _outputRequestJob[i];

          var requestOutput =
            input != default &&
            !outputRequested &&
            output == default;

          if (!requestOutput) continue;

          _outputRequestJob[i] = (input, true, default);

          var didRequest = _simData.RequestOutput(
            _simulation,
            input,
            this,
            i,
            persist: true
            );

          if (didRequest) break;

          _outputRequestJob[i] = (input, false, default);
        }
      }
    }

    private void ObserveSecondInterval(long _)
    {
      lock (_jobSyncLock)
      {
        RequireTrue(_runOutputRequestJob);
        RequireNotNull(_outputRequestJob);

        NOutputsAcquired = _outputRequestJob.Count(t => t.Input == default || t.Output != default);
        AcquireOutputsProgress = 100.0 * NOutputsAcquired / _outputRequestJob.Length;

        var isComplete = NOutputsAcquired == _outputRequestJob.Length;

        if (isComplete || (NOutputsAcquired - _moduleState.Outputs.Count) > 9)
        {
          _moduleState.Outputs = _outputRequestJob
            .Select((t, i) => (Index: i, t.Output))
            .Where(t => t.Output != default)
            .ToArr()!;

          _moduleState.OutputFilters = _moduleState.Outputs
            .Map(o => (o.Index, _moduleState.FilterConfig.IsInFilteredSet(o.Output)));
        }

        if (!isComplete) return;

        _runOutputRequestJob = false;
        _jobSubscriptions?.Dispose();
        _jobSubscriptions = default;

        RequireNotNull(_moduleState.SamplingDesign);

        if (_moduleState.SamplingDesign.NoDataIndices.IsEmpty)
        {
          var noDataIndices = _outputRequestJob
            .Select((t, i) => t.Output == default ? Some(i) : None)
            .Somes()
            .ToArr();

          if (!noDataIndices.IsEmpty)
          {
            var samplingDesign = _moduleState.SamplingDesign.With(noDataIndices);

            using (_reactiveSafeInvoke.SuspendedReactivity)
            {
              _samplingDesigns.Update(samplingDesign);
            }
          }
        }

        UpdateEnable();
      }
    }

    private void ObserveModuleStateSamples(object _)
    {
      Populate();
      UpdateEnable();
    }

    private void ObserveModuleStateSamplingDesign(object _)
    {
      UnloadCurrentDesign();
      Populate();

      if (_moduleState.SamplingDesign != default)
      {
        ConfigureOutputsAcquisition();
      }

      UpdateEnable();
    }

    private void Populate()
    {
      CreatedOn = _moduleState.SamplingDesign == default
        ? _moduleState.Samples == default
          ? default(DateTime?)
          : DateTime.Now
        : _moduleState.SamplingDesign.CreatedOn;
    }

    private void UnloadCurrentDesign()
    {
      lock (_jobSyncLock)
      {
        _runOutputRequestJob = false;
        _outputRequestJob = default;
        _jobSubscriptions?.Dispose();
      }

      CreatedOn = default;
      NOutputsToAcquire = default;
      NOutputsAcquired = default;
      AcquireOutputsProgress = default;
    }

    private void UpdateEnable()
    {
      CanCreateDesign =
        _moduleState.Samples != default &&
        _moduleState.SamplingDesign == default;

      CanUnloadDesign = _moduleState.SamplingDesign != default;

      CanAcquireOutputs =
        _outputRequestJob != default &&
        NOutputsAcquired < NOutputsToAcquire &&
        !_runOutputRequestJob;

      CanCancelAcquireOutputs = _runOutputRequestJob;
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          lock (_jobSyncLock)
          {
            _jobSubscriptions?.Dispose();
          }

          _subscriptions.Dispose();
        }

        _disposed = true;
      }
    }

    private void ConfigureOutputsAcquisition()
    {
      RequireNotNull(_moduleState.SamplingDesign);

      var outputRequestJob = CompileOutputRequestJob(
        _simulation,
        _simData,
        _moduleState.SamplingDesign.Samples,
        _moduleState.SamplingDesign.NoDataIndices
        );

      _moduleState.Outputs = outputRequestJob
        .Select((t, i) => (Index: i, t.Output))
        .Where(t => t.Output != default)
        .ToArr()!;

      _moduleState.OutputFilters = _moduleState.Outputs
        .Map(o => (o.Index, _moduleState.FilterConfig.IsInFilteredSet(o.Output)));

      NOutputsAcquired = outputRequestJob.Count(t =>
      {
        if (t.Input == default) return true;
        if (t.Output != default) return true;

        return false;
      });

      NOutputsToAcquire = outputRequestJob.Length;

      AcquireOutputsProgress = 100.0 * NOutputsAcquired / NOutputsToAcquire;

      var isComplete = NOutputsAcquired == NOutputsToAcquire;

      if (!isComplete)
      {
        _outputRequestJob = outputRequestJob;
      }
    }

    private static (SimInput Input, bool OutputRequested, NumDataTable? Output)[] CompileOutputRequestJob(
      Simulation simulation,
      ISimData simData,
      DataTable samples,
      Arr<int> noDataIndices
      )
    {
      var job = new (SimInput Input, bool OutputRequested, NumDataTable? Output)[samples.Rows.Count];
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

        var output = simData
          .GetOutput(input, simulation)
          .IfNoneUnsafe(default(NumDataTable)!);

        job[row] = (input, false, output);
      }

      return job;
    }

    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly SamplingDesigns _samplingDesigns;
    private readonly Simulation _simulation;
    private readonly ISimData _simData;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private (SimInput Input, bool OutputRequested, NumDataTable? Output)[]? _outputRequestJob;
    private bool _runOutputRequestJob;
    private IDisposable? _jobSubscriptions;
    private readonly object _jobSyncLock = new object();
    private bool _disposed = false;
  }
}
