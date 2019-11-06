using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
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
using static RVis.Base.Check;
using static RVis.Base.Extensions.CollExt;
using static RVis.Base.Extensions.LangExt;
using static RVis.Base.Extensions.NumExt;
using static Sensitivity.MeasuresOps;
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
        this.WhenAny(vm => vm.SampleSize, _ => SampleSize > 0)
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

      _subscriptions = new CompositeDisposable(

        moduleState
          .ObservableForProperty(ms => ms.ParameterStates)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveParameterStateChange
              )
            ),

        moduleState
          .ObservableForProperty(vm => vm.SensitivityDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSensitivityDesign
              )
            ),

        this
          .ObservableForProperty(vm => vm.SampleSize)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
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

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
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

    public int? SampleSize
    {
      get => _sampleSize;
      set => this.RaiseAndSetIfChanged(ref _sampleSize, value);
    }
    private int? _sampleSize;

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

    public DataView Inputs
    {
      get => _inputs;
      set => this.RaiseAndSetIfChanged(ref _inputs, value);
    }
    private DataView _inputs;

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

    private void HandleCreateDesign()
    {
      RequireTrue(SampleSize > 0);

      void SomeServer(ServerLicense serverLicense)
      {
        using (serverLicense)
        {
          using (_reactiveSafeInvoke.SuspendedReactivity)
          {
            try
            {
              var (samples, serializedDesign) = GetFast99Samples(
                _moduleState.ParameterStates,
                SampleSize.Value,
                serverLicense.Client
                );

              var parameterDistributions = _moduleState.ParameterStates
                .Filter(ps => ps.IsSelected)
                .OrderBy(ps => ps.Name.ToUpperInvariant())
                .Select(ps => (ps.Name, Distribution: ps.GetDistribution()))
                .ToArr();

              RequireTrue(parameterDistributions.ForAll(t => t.Distribution.IsConfigured));

              var sensitivityDesign = SensitivityDesign.CreateSensitivityDesign(
                DateTime.Now,
                serializedDesign,
                parameterDistributions,
                SampleSize.Value,
                samples
                );
              _sensitivityDesigns.Add(sensitivityDesign);

              UnloadCurrentDesign();

              _moduleState.SensitivityDesign = default;

              _moduleState.DesignOutputs = default;
              _moduleState.Trace = default;
              _moduleState.MeasuresState.OutputMeasures = default;
              _moduleState.SensitivityDesign = sensitivityDesign;

              var outputRequestJob = CompileOutputRequestJob(
                _simulation,
                _simData,
                samples,
                sensitivityDesign.DesignParameters.Filter(dp => dp.Distribution.DistributionType == DistributionType.Invariant)
                );

              NOutputsAcquired = outputRequestJob.Count(t => t.Input == default || t.Output != default);
              NOutputsToAcquire = samples.Rows.Count;
              if (NOutputsAcquired < NOutputsToAcquire) _outputRequestJob = outputRequestJob;

              var inputs = CreateInputs(samples);
              ShowIssues = false;
              UpdateInputs(inputs, outputRequestJob);

              DesignCreatedOn = sensitivityDesign.CreatedOn;

              var designOutputs = outputRequestJob
                .Select(t => t.Output)
                .ToArr();

              var canMeasure = designOutputs.ForAll(o => o != default);

              if (canMeasure)
              {
                if (designOutputs.NotAllSame(ndt => ndt.NRows))
                {
                  designOutputs = ToCommonIndependent(designOutputs);
                }

                var trace = designOutputs.Head();
                _moduleState.Trace = trace;
                _sensitivityDesigns.SaveTrace(sensitivityDesign, trace);

                PrepareInitialMeasures(designOutputs);
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

      void NoServer()
      {
        _appService.Notify(
          NotificationType.Information,
          nameof(DesignViewModel),
          nameof(HandleCreateDesign),
          "No R server available."
          );
      }

      _appService.RVisServerPool.RequestServer().Match(SomeServer, NoServer);
    }

    private void HandleUnloadDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        UnloadCurrentDesign();
        _moduleState.DesignOutputs = default;
        _moduleState.Trace = default;
        _moduleState.MeasuresState.OutputMeasures = default;
        _moduleState.SensitivityDesign = default;
        UpdateDesignEnable();
        UpdateSamplesEnable();
      }
    }

    private void HandleAcquireOutputs()
    {
      RequireNotNull(_outputRequestJob);
      RequireFalse(_runOutputRequestJob);
      RequireTrue(_moduleState.DesignOutputs.IsEmpty);
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

          if (input == default || outputRequested || output != default) continue;

          _outputRequestJob[i] = (input, true, default);

          var didRequest = _simData.RequestOutput(_simulation, input, this, i, true);

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

        _runOutputRequestJob = false;
        _jobSubscriptions.Dispose();
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
      RequireNotNull(_moduleState.SensitivityDesign);
      RequireFalse(SelectedInputIndex == NOT_FOUND);

      var defaultInput = _simulation.SimConfig.SimInput;

      var targetParameterNames = _moduleState.SensitivityDesign.Samples.Columns
        .Cast<DataColumn>()
        .Select(dc => dc.ColumnName)
        .ToArr();

      var dataRow = _moduleState.SensitivityDesign.Samples.Rows[SelectedInputIndex];

      var designStates = targetParameterNames.Map(n =>
      {
        var (_, minimum, maximum) = _appState.SimSharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
          n,
          defaultInput.SimParameters
          );

        var value = dataRow.Field<double>(n);
        if (value < minimum) minimum = value.GetPreviousOrderOfMagnitude();
        if (value > maximum) maximum = value.GetNextOrderOfMagnitude();

        return (n, value, minimum, maximum, NoneOf<IDistribution>());
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

      _appState.SimSharedState.ShareParameterState(designStates);
    }

    private void HandleViewError()
    {
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

        RequireTrue(outputRequest.RequestToken.Resolve(out int thisIndex));
        if (thisIndex >= _outputRequestJob.Length) return;

        var (input, _, output) = _outputRequestJob[thisIndex];
        if (input.Hash != outputRequest.Item.SeriesInput.Hash) return;
        if (output != default) return;

        outputRequest.Item.SerieInputs.Match(
          si =>
          {
            RequireTrue(si.Count == 1);
            output = _simData.GetOutput(si[0], _simulation).AssertSome();
          },
          ex =>
          {
            input = default;
            _outputRequestErrors.Add(thisIndex, ex);
          });

        _outputRequestJob[thisIndex] = (input, false, output);

        for (var index = thisIndex + 1; index < _outputRequestJob.Length; ++index)
        {
          if (_outputRequestJob[index].Input.Hash != outputRequest.Item.SeriesInput.Hash) continue;
          _outputRequestJob[index] = (input, false, output);
        }

        for (var index = 0; index < _outputRequestJob.Length; ++index)
        {
          if (_outputRequestJob[index].Input == default) continue;
          if (_outputRequestJob[index].OutputRequested) continue;
          if (_outputRequestJob[index].Output != default) continue;

          (input, _, _) = _outputRequestJob[index];
          _outputRequestJob[index] = (input, true, default);

          var didRequest = _simData.RequestOutput(_simulation, input, this, index, true);
          if (didRequest) break;

          _outputRequestJob[index] = (input, false, default);
        }
      }
    }

    private void ObserveSecondInterval(long _)
    {
      lock (_jobSyncLock)
      {
        RequireTrue(_runOutputRequestJob);

        NOutputsAcquired = _outputRequestJob.Count(t => t.Input == default || t.Output != default);
        AcquireOutputsProgress = 100.0 * NOutputsAcquired / _outputRequestJob.Length;

        var isComplete = NOutputsAcquired == _outputRequestJob.Length;
        var hasProducedErrors = 0 < _outputRequestErrors.Count;

        if (!isComplete && !hasProducedErrors) return;

        _runOutputRequestJob = false;
        _jobSubscriptions.Dispose();
        _jobSubscriptions = default;
        var outputRequestJob = _outputRequestJob;
        _outputRequestJob = default;

        UpdateInputs(Inputs.Table, outputRequestJob);

        if (hasProducedErrors)
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(DesignViewModel),
            nameof(ObserveSecondInterval),
            "Simulation produced one or more errors. Analysis not possible."
            );
        }
        else
        {
          var designOutputs = outputRequestJob
            .Select(t => t.Output)
            .ToArr();

          RequireFalse(designOutputs.Exists(o => o == default));

          if (designOutputs.NotAllSame(ndt => ndt.NRows))
          {
            designOutputs = ToCommonIndependent(designOutputs);
          }

          if (_moduleState.Trace == default)
          {
            var trace = designOutputs.Head();
            _sensitivityDesigns.SaveTrace(_moduleState.SensitivityDesign, trace);
            _moduleState.Trace = trace;
          }

          PrepareInitialMeasures(designOutputs);
        }

        UpdateDesignEnable();
        UpdateSamplesEnable();
      }
    }

    private void ObserveParameterStateChange(object _)
    {
      UnloadCurrentDesign();
      _moduleState.DesignOutputs = default;
      _moduleState.Trace = default;
      _moduleState.MeasuresState.OutputMeasures = default;
      _moduleState.SensitivityDesign = default;
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

    private void ObserveInputs(object _)
    {
      UnloadCurrentDesign();

      _moduleState.DesignOutputs = default;
      _moduleState.Trace = default;
      _moduleState.MeasuresState.OutputMeasures = default;
      _moduleState.SensitivityDesign = default;
      _moduleState.DesignState.SampleSize = SampleSize;

      UpdateDesignEnable();
    }

    private void ObserveSelectedInputIndex(object _)
    {
      UpdateSamplesEnable();
    }

    private void ObserveShowBadRows(object _)
    {
      SelectedInputIndex = NOT_FOUND;

      var inputs = Inputs.Table;
      Inputs = default;
      var dataView = inputs.DefaultView;
      dataView.RowFilter = InputsRowFilter;
      Inputs = dataView;

      UpdateSamplesEnable();
    }

    private string InputsRowFilter => ShowIssues
      ? $"[{ACQUIRED_DATACOLUMN_NAME}] = '{AcquiredType.Error.ToString()}' OR [{ACQUIRED_DATACOLUMN_NAME}] = '{AcquiredType.Suspect.ToString()}'"
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
      SampleSize = designState.SampleSize ?? 1000;
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
      RequireNull(_outputRequestJob);

      var sensitivityDesign = _moduleState.SensitivityDesign;

      SampleSize = sensitivityDesign.SampleSize;
      DesignCreatedOn = sensitivityDesign.CreatedOn;
      PopulateFactors(sensitivityDesign.DesignParameters);
      PopulateInvariants(sensitivityDesign.DesignParameters);
      NOutputsToAcquire = sensitivityDesign.Samples.Rows.Count;

      var outputRequestJob = CompileOutputRequestJob(
        _simulation,
        _simData,
        sensitivityDesign.Samples,
        sensitivityDesign.DesignParameters.Filter(dp => dp.Distribution.DistributionType == DistributionType.Invariant)
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
          var loadedMeasures = _sensitivityDesigns.LoadOutputMeasures(
            sensitivityDesign,
            _moduleState.MeasuresState.SelectedOutputName,
            out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
            );

          if (loadedMeasures)
          {
            _moduleState.MeasuresState.OutputMeasures =
              _moduleState.MeasuresState.OutputMeasures.Add(
                _moduleState.MeasuresState.SelectedOutputName,
                measures
                );
          }
        }
      }
      else
      {
        var designOutputs = outputRequestJob
          .Select(t => t.Output)
          .ToArr();

        RequireFalse(designOutputs.Exists(o => o == default));

        if (designOutputs.NotAllSame(ndt => ndt.NRows))
        {
          designOutputs = ToCommonIndependent(designOutputs);
        }

        if (_moduleState.Trace == default)
        {
          var trace = designOutputs.Head();
          _sensitivityDesigns.SaveTrace(_moduleState.SensitivityDesign, trace);
          _moduleState.Trace = trace;
        }

        PrepareInitialMeasures(designOutputs);
      }
    }

    private void UpdateInputs(DataTable inputs, (SimInput Input, bool OutputRequested, NumDataTable Output)[] outputRequestJob)
    {
      Inputs = default;
      HasIssues = UpdateInputsImpl(inputs, outputRequestJob);
      var dataView = inputs.DefaultView;
      dataView.RowFilter = InputsRowFilter;
      Inputs = dataView;
    }

    private void UnloadCurrentDesign()
    {
      _cancellationTokenSource?.Cancel();

      lock (_jobSyncLock)
      {
        _runOutputRequestJob = false;
        _outputRequestJob = default;
        _outputRequestErrors.Clear();
        _jobSubscriptions?.Dispose();
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
        var dataRowView = Inputs[SelectedInputIndex];
        var acquired = dataRowView.Row.Field<string>(ACQUIRED_DATACOLUMN_NAME);
        CanViewError = acquired == AcquiredType.Error.ToString();
      }
      else
      {
        CanViewError = false;
      }
    }

    private async Task GenerateOutputMeasuresAsync(string outputName, Arr<NumDataTable> designOutputs, ServerLicense serverLicense)
    {
      using (serverLicense)
      {
        _cancellationTokenSource = new CancellationTokenSource();

        TaskName = "Generate Output Measures";
        IsRunningTask = true;
        CanCancelTask = true;

        try
        {
          var measures = await Task.Run(
            () => GenerateOutputMeasures(
              outputName,
              _moduleState.SensitivityDesign.SerializedDesign,
              _moduleState.SensitivityDesign.Samples,
              designOutputs,
              serverLicense.Client,
              _cancellationTokenSource.Token,
              s => _appService.ScheduleLowPriorityAction(() => RaiseTaskMessageEvent(s))
            ),
            _cancellationTokenSource.Token
            );

          _moduleState.MeasuresState.SelectedOutputName = outputName;
          _moduleState.DesignOutputs = designOutputs;
          _moduleState.MeasuresState.OutputMeasures =
            _moduleState.MeasuresState.OutputMeasures.Add(outputName, measures);

          UpdateDesignEnable();
          UpdateSamplesEnable();

          _sensitivityDesigns.SaveOutputMeasures(
            _moduleState.SensitivityDesign,
            outputName,
            measures.FirstOrder,
            measures.TotalOrder,
            measures.Variance
            );
        }
        catch (OperationCanceledException)
        {
          // expected
        }
        catch (Exception ex)
        {
          _appService.Notify(
            nameof(DesignViewModel),
            nameof(GenerateOutputMeasuresAsync),
            ex
            );
        }

        IsRunningTask = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = default;
      }
    }

    private void PrepareInitialMeasures(Arr<NumDataTable> designOutputs)
    {
      RequireTrue(_moduleState.DesignOutputs.IsEmpty);

      var outputName = _moduleState.MeasuresState.SelectedOutputName;
      if (outputName.IsntAString())
      {
        outputName = _simulation.SimConfig.SimOutput.DependentVariables.Head().Name;
      }

      if (_moduleState.MeasuresState.OutputMeasures.ContainsKey(outputName))
      {
        _moduleState.MeasuresState.SelectedOutputName = outputName;
        _moduleState.DesignOutputs = designOutputs;
        return;
      }

      var loadedMeasures = _sensitivityDesigns.LoadOutputMeasures(
        _moduleState.SensitivityDesign,
        outputName,
        out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
        );

      if (loadedMeasures)
      {
        _moduleState.MeasuresState.SelectedOutputName = outputName;
        _moduleState.DesignOutputs = designOutputs;
        _moduleState.MeasuresState.OutputMeasures =
          _moduleState.MeasuresState.OutputMeasures.Add(outputName, measures);
        return;
      }

      void SomeServer(ServerLicense serverLicense)
      {
        var _ = GenerateOutputMeasuresAsync(outputName, designOutputs, serverLicense);
      }

      void NoServer()
      {
        _appService.Notify(
          NotificationType.Information,
          nameof(DesignViewModel),
          nameof(PrepareInitialMeasures),
          "No R server available."
          );
      }

      _appService.RVisServerPool.RequestServer().Match(SomeServer, NoServer);
    }

    private enum AcquiredType
    {
      Error,
      Suspect,
      No,
      Yes
    }

    private const string ACQUIRED_DATACOLUMN_NAME = "Acquired?";

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly SensitivityDesigns _sensitivityDesigns;
    private readonly ISimData _simData;
    private readonly Simulation _simulation;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private CancellationTokenSource _cancellationTokenSource;
    private (SimInput Input, bool OutputRequested, NumDataTable Output)[] _outputRequestJob;
    private bool _runOutputRequestJob;
    private readonly IDictionary<int, Exception> _outputRequestErrors = new SortedDictionary<int, Exception>();
    private IDisposable _jobSubscriptions;
    private readonly object _jobSyncLock = new object();
    private bool _disposed = false;
  }
}
