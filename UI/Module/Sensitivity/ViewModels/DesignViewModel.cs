using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.LangExt;
using static RVis.Base.Extensions.NumExt;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class DesignViewModel : ViewModelBase, IDesignViewModel
  {
    internal DesignViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      ModuleState moduleState,
      SensitivityDesigns sensitivityDesigns
      )
    {
      _appState = appState;
      _appService = appService;
      _appSettings = appSettings;
      _moduleState = moduleState;
      _sensitivityDesigns = sensitivityDesigns;
      _simData = appState.SimData;
      _simulation = _appState.Target.AssertSome();

      CreateDesign = ReactiveCommand.Create(
        HandleCreateDesign,
        this.WhenAny(
          vm => vm.SensitivityMethod,
          vm => vm.NoOfRuns,
          vm => vm.NoOfSamples,
          (_, __, ___) =>
            (SensitivityMethod == SensitivityMethod.Morris && NoOfRuns > 0) ||
            (SensitivityMethod == SensitivityMethod.Fast99 && NoOfSamples > 0))
        );

      UnloadDesign = ReactiveCommand.Create(HandleUnloadDesign);

      AcquireOutputs = ReactiveCommand.Create(
        HandleAcquireOutputs,
        this.WhenAny(vm => vm.CanAcquireOutputs, _ => CanAcquireOutputs)
        );

      CancelAcquireOutputs = ReactiveCommand.Create(
        HandleCancelAcquireOutputs,
        this.WhenAny(vm => vm.CanCancelAcquireOutputs, _ => CanCancelAcquireOutputs)
        );

      ShareParameters = ReactiveCommand.Create(
        HandleShareParameters,
        this.WhenAny(vm => vm.CanShareParameters, _ => CanShareParameters)
        );

      ViewError = ReactiveCommand.Create(
        HandleViewError,
        this.WhenAny(vm => vm.CanViewError, _ => CanViewError)
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _subscriptions = new CompositeDisposable(

        moduleState
          .ObservableForProperty(ms => ms.ParameterStates)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveParameterStateChange
              )
            ),

        moduleState
          .ObservableForProperty(ms => ms.SensitivityDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSensitivityDesign
              )
            ),

        moduleState.MeasuresState
          .ObservableForProperty(ms => ms.SelectedOutputName)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveMeasuresStateSelectedOutputName
              )
            ),

        this
          .WhenAnyValue(
            vm => vm.SensitivityMethod,
            vm => vm.NoOfRuns,
            vm => vm.NoOfSamples
            )
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<(SensitivityMethod, int?, int?)>(
              ObserveInputs
              )
          ),

        this
          .ObservableForProperty(vm => vm.SelectedInputIndex)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSelectedInputIndex
              )
           ),

        this
          .ObservableForProperty(vm => vm.ShowIssues)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveShowBadRows
              )
           )

        );

        if (_moduleState.SensitivityDesign == default)
        {
          Populate(
            _moduleState.DesignState,
            _moduleState.ParameterStates
            );
        }
        else
        {
          Populate();
        }

        UpdateDesignEnable();
        UpdateSamplesEnable();
      }
    }

    public Arr<string> Factors
    {
      get => _factors;
      set => this.RaiseAndSetIfChanged(ref _factors, value);
    }
    private Arr<string> _factors;

    public Arr<string> Invariants
    {
      get => _invariants;
      set => this.RaiseAndSetIfChanged(ref _invariants, value);
    }
    private Arr<string> _invariants;

    public SensitivityMethod SensitivityMethod
    {
      get => _sensitivityMethod;
      set => this.RaiseAndSetIfChanged(ref _sensitivityMethod, value);
    }
    private SensitivityMethod _sensitivityMethod;

    public int? NoOfRuns
    {
      get => _noOfRuns;
      set => this.RaiseAndSetIfChanged(ref _noOfRuns, value);
    }
    private int? _noOfRuns;

    public int? NoOfSamples
    {
      get => _noOfSamples;
      set => this.RaiseAndSetIfChanged(ref _noOfSamples, value);
    }
    private int? _noOfSamples;

    public ICommand CreateDesign { get; }

    public bool CanCreateDesign
    {
      get => _canCreateDesign;
      set => this.RaiseAndSetIfChanged(ref _canCreateDesign, value);
    }
    private bool _canCreateDesign;

    public DateTime? DesignCreatedOn
    {
      get => _designCreatedOn;
      set => this.RaiseAndSetIfChanged(ref _designCreatedOn, value);
    }
    private DateTime? _designCreatedOn = DateTime.UtcNow;

    public ICommand UnloadDesign { get; }

    public bool CanUnloadDesign
    {
      get => _canUnloadDesign;
      set => this.RaiseAndSetIfChanged(ref _canUnloadDesign, value);
    }
    private bool _canUnloadDesign;

    public double AcquireOutputsProgress
    {
      get => _acquireOutputsProgress;
      set => this.RaiseAndSetIfChanged(ref _acquireOutputsProgress, value);
    }
    private double _acquireOutputsProgress;

    public int NOutputsAcquired
    {
      get => _nOutputsAcquired;
      set => this.RaiseAndSetIfChanged(ref _nOutputsAcquired, value);
    }
    private int _nOutputsAcquired;

    public int NOutputsToAcquire
    {
      get => _nOutputsToAcquire;
      set => this.RaiseAndSetIfChanged(ref _nOutputsToAcquire, value);
    }
    private int _nOutputsToAcquire;

    public ICommand AcquireOutputs { get; }

    public bool CanAcquireOutputs
    {
      get => _canAcquireOutputs;
      set => this.RaiseAndSetIfChanged(ref _canAcquireOutputs, value);
    }
    private bool _canAcquireOutputs;

    public ICommand CancelAcquireOutputs { get; }

    public bool CanCancelAcquireOutputs
    {
      get => _canCancelAcquireOutputs;
      set => this.RaiseAndSetIfChanged(ref _canCancelAcquireOutputs, value);
    }
    private bool _canCancelAcquireOutputs;

    public DataView? Inputs
    {
      get => _inputs;
      set => this.RaiseAndSetIfChanged(ref _inputs, value);
    }
    private DataView? _inputs;

    public int SelectedInputIndex
    {
      get => _selectedInputIndex;
      set => this.RaiseAndSetIfChanged(ref _selectedInputIndex, value);
    }
    private int _selectedInputIndex = NOT_FOUND;

    public ICommand ShareParameters { get; }

    public bool CanShareParameters
    {
      get => _canShareParameters;
      set => this.RaiseAndSetIfChanged(ref _canShareParameters, value);
    }
    private bool _canShareParameters;

    public ICommand ViewError { get; }

    public bool CanViewError
    {
      get => _canViewError;
      set => this.RaiseAndSetIfChanged(ref _canViewError, value);
    }
    private bool _canViewError;

    public bool ShowIssues
    {
      get => _showIssues;
      set => this.RaiseAndSetIfChanged(ref _showIssues, value);
    }
    private bool _showIssues;

    public bool HasIssues
    {
      get => _hasIssues;
      set => this.RaiseAndSetIfChanged(ref _hasIssues, value);
    }
    private bool _hasIssues;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
    private bool _isSelected;

    public override void HandleCancelTask()
    {
      _cancellationTokenSource?.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          lock (_jobSyncLock)
          {
            _jobSubscriptions?.Dispose();

            if (_cancellationTokenSource?.IsCancellationRequested == false)
            {
              _cancellationTokenSource?.Cancel();
              _cancellationTokenSource?.Dispose();
            }
          }

          _subscriptions.Dispose();
        }

        _disposed = true;
      }

      base.Dispose(disposing);
    }

    private async Task HandleCreateDesign()
    {
      var parameterDistributions = _moduleState.ParameterStates
        .Filter(ps => ps.IsSelected)
        .OrderBy(ps => ps.Name.ToUpperInvariant())
        .Select(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .ToArr();

      RequireFalse(
        parameterDistributions.Count == 0,
        "Configure one or more parameter distributions."
        );

      RequireTrue(
        parameterDistributions.ForAll(t => t.Distribution.IsConfigured),
        "One or more parameter distribitions not correctly configured"
        );

      async Task SomeServer(ServerLicense serverLicense)
      {
        using (serverLicense)
        {
          using (_reactiveSafeInvoke.SuspendedReactivity)
          {
            try
            {
              if (SensitivityMethod == SensitivityMethod.Morris)
              {
                await CreateMorrisDesignAsync(
                  parameterDistributions,
                  await serverLicense.GetRClientAsync()
                  );
              }
              else
              {
                await CreateFast99DesignAsync(
                  parameterDistributions,
                  await serverLicense.GetRClientAsync()
                  );
              }
            }
            catch (Exception ex)
            {
              _appService.Notify(
                nameof(DesignViewModel),
                nameof(HandleCreateDesign),
                ex
                );
            }

            UpdateDesignEnable();
            UpdateSamplesEnable();
          }
        }
      }

      Task NoServer()
      {
        _appService.Notify(
          NotificationType.Information,
          nameof(DesignViewModel),
          nameof(HandleCreateDesign),
          "No R server available."
          );

        return Task.CompletedTask;
      }

      await _appService.RVisServerPool
        .RequestServer()
        .Match(SomeServer, NoServer);
    }

    private void HandleUnloadDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        UnloadCurrentDesign();
        _moduleState.Ranking = default;
        _moduleState.Trace = default;
        _moduleState.MeasuresState.MorrisOutputMeasures = default;
        _moduleState.MeasuresState.Fast99OutputMeasures = default;
        _moduleState.SensitivityDesign = default;
        UpdateDesignEnable();
        UpdateSamplesEnable();
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

      UpdateDesignEnable();

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
        RequireNotNull(Inputs?.Table);

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

        UpdateInputs(Inputs.Table, _outputRequestJob);

        UpdateDesignEnable();
      }
    }

    private void HandleShareParameters()
    {
      RequireNotNull(Inputs?.Table);
      RequireNotNull(_moduleState.SensitivityDesign);
      RequireFalse(SelectedInputIndex == NOT_FOUND);

      var defaultInput = _simulation.SimConfig.SimInput;

      var targetParameterNames = Inputs.Table.Columns
        .Cast<DataColumn>()
        .Select(dc => dc.ColumnName)
        .ToArr();

      var dataRow = Inputs.Table.Rows[SelectedInputIndex];

      var designStates = targetParameterNames.Map(n =>
      {
        var (_, minimum, maximum) = _appState.SimSharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
          n,
          defaultInput.SimParameters
          );

        var value = dataRow.Field<double>(n);
        if (value < minimum) minimum = value.GetPreviousOrderOfMagnitude();
        if (value > maximum) maximum = value.GetNextOrderOfMagnitude();

        return (Name: n, value, minimum, maximum, NoneOf<IDistribution>());
      });

      var sample = new double[1];
      var invariantStates = _moduleState.SensitivityDesign.DesignParameters
        .Filter(dp => dp.Distribution.DistributionType == DistributionType.Invariant)
        .Map(dp =>
        {
          var (_, minimum, maximum) = _appState.SimSharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
            dp.Name,
            defaultInput.SimParameters
            );

          dp.Distribution.FillSamples(sample);
          var value = sample[0];
          if (value < minimum) minimum = value.GetPreviousOrderOfMagnitude();
          if (value > maximum) maximum = value.GetNextOrderOfMagnitude();

          return (dp.Name, value, minimum, maximum, NoneOf<IDistribution>());
        });

      _appState.SimSharedState.ShareParameterState(designStates + invariantStates);
    }

    private void HandleViewError()
    {
      RequireNotNull(Inputs?.Table);

      var dataRowView = Inputs[SelectedInputIndex];
      var index = Inputs.Table.Rows.IndexOf(dataRowView.Row);
      RequireTrue(_outputRequestErrors.ContainsKey(index));
      var exception = _outputRequestErrors[index];
      _appService.Notify(
        nameof(DesignViewModel),
        nameof(HandleViewError),
        exception
        );
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
          RequireTrue(_moduleState.MeasuresState.SelectedOutputName.IsAString());

          var numDataTable = _simData
            .GetOutput(serieInputs[0], _simulation)
            .AssertSome();

          if (_moduleState.Trace == default && thisIndex == 0)
          {
            var independentData = _simulation.SimConfig.SimOutput.GetIndependentData(numDataTable);

            var unique = independentData.Data.AllUnique(d => d);
            var ascending = Range(1, independentData.Length - 1)
              .ForAll(i => independentData[i - 1] < independentData[i]);

            if (unique && ascending)
            {
              RequireNotNull(_moduleState.SensitivityDesign);

              _moduleState.Trace = numDataTable;

              _sensitivityDesigns.SaveTrace(
                _moduleState.SensitivityDesign,
                numDataTable
                );
            }
            else
            {
              thisInput = default;
              _outputRequestErrors.Add(
                thisIndex,
                new Exception($"{independentData.Name} column does not contain unique and ascending values")
                );
            }
          }

          if (_moduleState.Trace != default && thisIndex > 0)
          {
            var idTrace = _simulation.SimConfig.SimOutput.GetIndependentData(_moduleState.Trace);
            var idThis = _simulation.SimConfig.SimOutput.GetIndependentData(numDataTable);

            if (idThis.Length != idTrace.Length)
            {
              thisInput = default;
              _outputRequestErrors.Add(
                thisIndex,
                new Exception($"Unexpected output length: {idThis.Length} (expected {idTrace.Length})")
                );
            }
          }

          if (thisInput != default)
          {
            thisOutput = numDataTable[_moduleState.MeasuresState.SelectedOutputName].Data.ToArr();
          }
        }

        void RequestFailed(Exception ex)
        {
          thisInput = default;
          _outputRequestErrors.Add(thisIndex, ex);
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
        RequireNotNull(Inputs?.Table);

        NOutputsAcquired = _outputRequestJob.Count(t => t.Input == default || t.Output != default);
        AcquireOutputsProgress = 100.0 * NOutputsAcquired / _outputRequestJob.Length;

        var isComplete = NOutputsAcquired == _outputRequestJob.Length;
        var hasProducedErrors = 0 < _outputRequestErrors.Count;

        if (!isComplete && !hasProducedErrors) return;

        _runOutputRequestJob = false;
        _jobSubscriptions?.Dispose();
        _jobSubscriptions = default;
        var outputRequestJob = _outputRequestJob;
        _outputRequestJob = default;

        if (_moduleState.Trace != default)
        {
          var expectedOutputLength = _moduleState.Trace.NRows;

          for (var i = 1; i < outputRequestJob.Length; ++i)
          {
            if (_outputRequestErrors.ContainsKey(i)) continue;

            var output = outputRequestJob[i].Output;
            if (output == default) continue;

            var actualOutputLength = output.Count;

            if (actualOutputLength == expectedOutputLength) continue;

            _outputRequestErrors.Add(
              i,
              new InvalidOperationException(
                $"Unexpected output length: {actualOutputLength} (expected {expectedOutputLength})"
                )
              );

            outputRequestJob[i] = (default, false, default);

            hasProducedErrors = true;
          }
        }

        UpdateInputs(Inputs.Table, outputRequestJob);

        if (hasProducedErrors)
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(DesignViewModel),
            nameof(ObserveSecondInterval),
            "Simulation produced one or more errors. See grid for details. Analysis not possible."
            );
        }
        else
        {
          var designOutputs = outputRequestJob
            .Select(t => t.Output)
            .ToArr();

          RequireFalse(designOutputs.Exists(o => o.IsEmpty));

          Measure(designOutputs);
        }

        UpdateDesignEnable();
        UpdateSamplesEnable();
      }
    }

    private void ObserveParameterStateChange(object _)
    {
      PopulateFactors(_moduleState.ParameterStates);
      PopulateInvariants(_moduleState.ParameterStates);
      UpdateDesignEnable();
    }

    private void ObserveModuleStateSensitivityDesign(object _)
    {
      UnloadCurrentDesign();

      if (_moduleState.SensitivityDesign == default)
      {
        Populate(_moduleState.DesignState, _moduleState.ParameterStates);
      }
      else
      {
        Populate();
      }

      UpdateDesignEnable();
      UpdateSamplesEnable();
    }

    private void ObserveMeasuresStateSelectedOutputName(object _)
    {
      if (_moduleState.MeasuresState.SelectedOutputName.IsntAString()) return;

      RequireNotNull(_moduleState.SensitivityDesign);

      if (_moduleState.SensitivityDesign.SensitivityMethod == SensitivityMethod.Morris)
      {
        var alreadyAcquired = _moduleState.MeasuresState.MorrisOutputMeasures.ContainsKey(
          _moduleState.MeasuresState.SelectedOutputName
          );
        if (alreadyAcquired) return;

        var loadedMeasures = _sensitivityDesigns.LoadMorrisOutputMeasures(
          _moduleState.SensitivityDesign,
          _moduleState.MeasuresState.SelectedOutputName,
          out (DataTable Mu, DataTable MuStar, DataTable Sigma) measures
          );

        if (loadedMeasures)
        {
          _moduleState.MeasuresState.MorrisOutputMeasures =
            _moduleState.MeasuresState.MorrisOutputMeasures.Add(
              _moduleState.MeasuresState.SelectedOutputName,
              measures
              );
          return;
        }
      }
      else
      {
        var alreadyAcquired = _moduleState.MeasuresState.Fast99OutputMeasures.ContainsKey(
          _moduleState.MeasuresState.SelectedOutputName
          );
        if (alreadyAcquired) return;

        var loadedMeasures = _sensitivityDesigns.LoadFast99OutputMeasures(
          _moduleState.SensitivityDesign,
          _moduleState.MeasuresState.SelectedOutputName,
          out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
          );

        if (loadedMeasures)
        {
          _moduleState.MeasuresState.Fast99OutputMeasures =
            _moduleState.MeasuresState.Fast99OutputMeasures.Add(
              _moduleState.MeasuresState.SelectedOutputName,
              measures
              );
          return;
        }
      }

      RequireFalse(_sampleInputs.IsEmpty);

      var outputRequestJob = CompileOutputRequestJob(
        _moduleState.MeasuresState.SelectedOutputName,
        _simulation,
        _simData,
        _sampleInputs
        );

      NOutputsAcquired = outputRequestJob.Count(t => t.Input == default || t.Output != default);
      AcquireOutputsProgress = 100.0 * NOutputsAcquired / outputRequestJob.Length;

      if (NOutputsAcquired < NOutputsToAcquire)
      {
        _outputRequestJob = outputRequestJob;
        HandleAcquireOutputs();
      }
      else
      {
        var designOutputs = outputRequestJob
          .Select(t => t.Output)
          .ToArr();

        RequireFalse(designOutputs.Exists(o => o == default));

        Measure(designOutputs);
      }
    }

    private void ObserveInputs((SensitivityMethod, int?, int?) _)
    {
      UpdateDesignEnable();
    }

    private void ObserveSelectedInputIndex(object _)
    {
      UpdateSamplesEnable();
    }

    private void ObserveShowBadRows(object _)
    {
      SelectedInputIndex = NOT_FOUND;

      RequireNotNull(Inputs?.Table);

      var inputs = Inputs.Table;
      Inputs = default;
      var dataView = inputs.DefaultView;
      dataView.RowFilter = InputsRowFilter;
      Inputs = dataView;

      UpdateSamplesEnable();
    }

    private string? InputsRowFilter => ShowIssues
      ? $"[{ACQUIRED_DATACOLUMN_NAME}] = '{AcquiredType.Error}' OR [{ACQUIRED_DATACOLUMN_NAME}] = '{AcquiredType.Suspect}'"
      : default;

    private void PopulateFactors(Arr<ParameterState> parameterStates) =>
      Factors = parameterStates
        .Filter(ps => ps.IsSelected)
        .Map(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .Filter(t => t.Distribution.DistributionType != DistributionType.Invariant)
        .OrderBy(t => t.Name.ToUpperInvariant())
        .Select(t => t.Distribution.ToString(t.Name))
        .ToArr();

    private void PopulateFactors(Arr<DesignParameter> designParameters) =>
      Factors = designParameters
        .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
        .OrderBy(dp => dp.Name.ToUpperInvariant())
        .Select(dp => dp.Distribution.ToString(dp.Name))
        .ToArr();

    private void PopulateInvariants(Arr<ParameterState> parameterStates) =>
      Invariants = parameterStates
        .Filter(ps => ps.IsSelected)
        .Map(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .Filter(t => t.Distribution.DistributionType == DistributionType.Invariant)
        .OrderBy(t => t.Name.ToUpperInvariant())
        .Select(t => t.Distribution.ToString(t.Name))
        .ToArr();

    private void PopulateInvariants(Arr<DesignParameter> designParameters) =>
      Invariants = designParameters
        .Filter(dp => dp.Distribution.DistributionType == DistributionType.Invariant)
        .OrderBy(dp => dp.Name.ToUpperInvariant())
        .Select(dp => dp.Distribution.ToString(dp.Name))
        .ToArr();

    private void Populate(DesignState designState, Arr<ParameterState> parameterStates)
    {
      SensitivityMethod = designState.SensitivityMethod ?? SensitivityMethod.Morris;
      NoOfRuns = designState.NoOfRuns ?? 6;
      NoOfSamples = designState.NoOfSamples ?? 1000;
      DesignCreatedOn = default;
      PopulateFactors(parameterStates);
      PopulateInvariants(parameterStates);
      NOutputsToAcquire = 0;
      Inputs = default;
      ShowIssues = false;
      HasIssues = false;
      NOutputsAcquired = 0;
      AcquireOutputsProgress = 0.0;
    }

    private void Populate()
    {
      RequireNotNull(_moduleState.SensitivityDesign);
      RequireTrue(_moduleState.MeasuresState.SelectedOutputName.IsAString());
      RequireNull(_outputRequestJob);

      var sensitivityDesign = _moduleState.SensitivityDesign;

      var methodParameters = sensitivityDesign.MethodParameters.ToMethodParameters();

      if (sensitivityDesign.SensitivityMethod == SensitivityMethod.Morris)
      {
        SensitivityMethod = SensitivityMethod.Morris;

        RequireTrue(int.TryParse(
          methodParameters.Snd(nameof(NoOfRuns)),
          out int noOfRuns
          ));
        NoOfRuns = noOfRuns;
      }
      else
      {
        SensitivityMethod = SensitivityMethod.Fast99;

        RequireTrue(int.TryParse(
          methodParameters.Snd(nameof(NoOfSamples)),
          out int noOfSamples
          ));
        NoOfSamples = noOfSamples;
      }

      DesignCreatedOn = sensitivityDesign.CreatedOn;
      PopulateFactors(sensitivityDesign.DesignParameters);
      PopulateInvariants(sensitivityDesign.DesignParameters);
      NOutputsToAcquire = sensitivityDesign.Samples.Sum(dt => dt.Rows.Count);

      _sampleInputs = CompileSampleInputs(
        _simulation,
        sensitivityDesign.Samples,
        sensitivityDesign.DesignParameters.Filter(
          dp => dp.Distribution.DistributionType == DistributionType.Invariant
          )
        );

      var outputRequestJob = CompileOutputRequestJob(
        _moduleState.MeasuresState.SelectedOutputName,
        _simulation,
        _simData,
        _sampleInputs
        );

      var inputs = CreateInputs(sensitivityDesign.Samples);
      UpdateInputs(inputs, outputRequestJob);

      NOutputsAcquired = outputRequestJob.Count(t => t.Input == default || t.Output != default);
      AcquireOutputsProgress = 100.0 * NOutputsAcquired / outputRequestJob.Length;

      if (NOutputsAcquired < NOutputsToAcquire)
      {
        _outputRequestJob = outputRequestJob;

        if (_moduleState.MeasuresState.SelectedOutputName.IsAString())
        {
          if (sensitivityDesign.SensitivityMethod == SensitivityMethod.Morris)
          {
            var loadedMeasures = _sensitivityDesigns.LoadMorrisOutputMeasures(
              sensitivityDesign,
              _moduleState.MeasuresState.SelectedOutputName,
              out (DataTable Mu, DataTable MuStar, DataTable Sigma) measures
              );

            if (loadedMeasures)
            {
              _moduleState.MeasuresState.MorrisOutputMeasures =
                _moduleState.MeasuresState.MorrisOutputMeasures.Add(
                  _moduleState.MeasuresState.SelectedOutputName,
                  measures
                  );
            }
          }
          else
          {
            var loadedMeasures = _sensitivityDesigns.LoadFast99OutputMeasures(
              sensitivityDesign,
              _moduleState.MeasuresState.SelectedOutputName,
              out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
              );

            if (loadedMeasures)
            {
              _moduleState.MeasuresState.Fast99OutputMeasures =
                _moduleState.MeasuresState.Fast99OutputMeasures.Add(
                  _moduleState.MeasuresState.SelectedOutputName,
                  measures
                  );
            }
          }
        }
      }
      else
      {
        var designOutputs = outputRequestJob
          .Select(t => t.Output)
          .ToArr();

        RequireFalse(designOutputs.Exists(o => o == default));

        Measure(designOutputs);
      }
    }

    private void UnloadCurrentDesign()
    {
      _cancellationTokenSource?.Cancel();

      lock (_jobSyncLock)
      {
        _runOutputRequestJob = false;
        _sampleInputs = default;
        _outputRequestJob = default;
        _outputRequestErrors.Clear();
        _jobSubscriptions?.Dispose();
        _jobSubscriptions = default;
      }

      DesignCreatedOn = default;
      Inputs = default;
      ShowIssues = false;
      HasIssues = false;
      SelectedInputIndex = NOT_FOUND;
      AcquireOutputsProgress = 0.0;
      NOutputsAcquired = 0;
      NOutputsToAcquire = 0;
    }

    private void UpdateDesignEnable()
    {
      CanCreateDesign = _moduleState.SensitivityDesign == default;
      CanUnloadDesign = _moduleState.SensitivityDesign != default;
      CanAcquireOutputs =
        _outputRequestJob != default &&
        !_runOutputRequestJob &&
        _outputRequestErrors.Count == 0;
      CanCancelAcquireOutputs = _runOutputRequestJob;
    }

    private void UpdateSamplesEnable()
    {
      var haveInputSelection = SelectedInputIndex != NOT_FOUND;
      CanShareParameters = haveInputSelection;

      if (haveInputSelection)
      {
        RequireNotNull(Inputs);

        var dataRowView = Inputs[SelectedInputIndex];
        var acquired = dataRowView.Row.Field<string>(ACQUIRED_DATACOLUMN_NAME);
        CanViewError = acquired == AcquiredType.Error.ToString();
      }
      else
      {
        CanViewError = false;
      }
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly SensitivityDesigns _sensitivityDesigns;
    private readonly ISimData _simData;
    private readonly Simulation _simulation;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private CancellationTokenSource? _cancellationTokenSource;
    private Arr<SimInput> _sampleInputs;
    private (SimInput Input, bool OutputRequested, Arr<double> Output)[]? _outputRequestJob;
    private bool _runOutputRequestJob;
    private readonly IDictionary<int, Exception> _outputRequestErrors = new SortedDictionary<int, Exception>();
    private IDisposable? _jobSubscriptions;
    private readonly object _jobSyncLock = new object();
    private bool _disposed = false;
  }
}
