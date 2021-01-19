using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using System;
using System.Windows.Input;
using static System.Linq.Enumerable;
using static RVis.Base.Check;

namespace RVisUI.Mvvm
{
  public class SharedStateViewModel : ReactiveObject, ISharedStateViewModel
  {
    public SharedStateViewModel(IAppState appState, IAppService appService)
    {
      _appState = appState;

      OpenView = ReactiveCommand.Create(
        HandleOpenView,
        this.WhenAny(
          vm => vm.IsViewOpen,
          vm => vm.ActiveSharedStateProvider,
          (_, __) => !_isViewOpen && _activeSharedStateProvider != null
          )
        );

      ApplyParametersState = ReactiveCommand.Create(HandleApplyParametersState);
      ShareParametersState = ReactiveCommand.Create(HandleShareParametersState);
      ApplyOutputsState = ReactiveCommand.Create(HandleApplyOutputsState);
      ShareOutputsState = ReactiveCommand.Create(HandleShareOutputsState);
      ApplyObservationsState = ReactiveCommand.Create(HandleApplyObservationsState);
      ShareObservationsState = ReactiveCommand.Create(HandleShareObservationsState);
      ApplyState = ReactiveCommand.Create(HandleApplyState);
      ShareState = ReactiveCommand.Create(HandleShareState);

      _applySingleParameterState = ReactiveCommand.Create<SimParameterSharedState>(HandleApplySingleParameterState);
      _applySingleOutputState = ReactiveCommand.Create<SimElementSharedState>(HandleApplySingleOutputState);
      _applySingleObservationsState = ReactiveCommand.Create<SimObservations>(HandleApplySingleObservationsState);

      CloseView = ReactiveCommand.Create(HandleCloseView);

      _appState
        .ObservableForProperty(s => s.ActiveUIComponent)
        .Subscribe(appService.SafeInvoke<object>(ObserveActiveUIComponent));

      _appState.SimSharedState
        .ObservableForProperty(ss => ss.ParameterSharedStates)
        .Subscribe(appService.SafeInvoke<object>(ObserveParameterSharedStates));

      _appState.SimSharedState
        .ObservableForProperty(ss => ss.ElementSharedStates)
        .Subscribe(appService.SafeInvoke<object>(ObserveElementSharedStates));

      _appState.SimSharedState
        .ObservableForProperty(ss => ss.ObservationsSharedStates)
        .Subscribe(appService.SafeInvoke<object>(ObserveObservationsSharedStates));

      UpdateSharedStateTarget();
      UpdateSharedParameters();
      UpdateSharedOutputs();
      UpdateSharedObservations();
    }

    public ICommand OpenView { get; }

    public ICommand CloseView { get; }

    public bool IsViewOpen
    {
      get => _isViewOpen;
      set => this.RaiseAndSetIfChanged(ref _isViewOpen, value);
    }
    private bool _isViewOpen;

    public object?[][]? SharedParameters
    {
      get => _sharedParameters;
      set => this.RaiseAndSetIfChanged(ref _sharedParameters, value);
    }
    private object?[][]? _sharedParameters;

    public ICommand ApplyParametersState { get; }

    public ICommand ShareParametersState { get; }

    public object[][]? SharedOutputs
    {
      get => _sharedOutputs;
      set => this.RaiseAndSetIfChanged(ref _sharedOutputs, value);
    }
    private object[][]? _sharedOutputs;

    public ICommand ApplyOutputsState { get; }

    public ICommand ShareOutputsState { get; }

    public object[][]? SharedObservations
    {
      get => _sharedObservations;
      set => this.RaiseAndSetIfChanged(ref _sharedObservations, value);
    }
    private object[][]? _sharedObservations;

    public ICommand ApplyObservationsState { get; }

    public ICommand ShareObservationsState { get; }

    public ICommand ApplyState { get; }

    public ICommand ShareState { get; }

    public ISharedStateProvider? ActiveSharedStateProvider
    {
      get => _activeSharedStateProvider;
      set => this.RaiseAndSetIfChanged(ref _activeSharedStateProvider, value);
    }
    private ISharedStateProvider? _activeSharedStateProvider;

    private void HandleOpenView()
    {
      IsViewOpen = true;
    }

    private void HandleApplyParametersState()
    {
      RequireNotNull(ActiveSharedStateProvider);

      var simulation = _appState.Target.AssertSome();
      var input = simulation.SimConfig.SimInput;
      var parameterState = _appState.SimSharedState.ParameterSharedStates.Map(
        pss => (
          input.SimParameters.GetParameter(pss.Name).With(pss.Value),
          pss.Minimum,
          pss.Maximum,
          pss.Distribution
          )
        );

      ActiveSharedStateProvider.ApplyState(
        SimSharedStateApply.Parameters | SimSharedStateApply.Set,
        parameterState,
        default,
        default
        );
    }

    private void HandleShareParametersState() => HandleShareState(SimSharedStateBuild.Parameters);

    private void HandleApplyOutputsState()
    {
      RequireNotNull(ActiveSharedStateProvider);

      var simulation = _appState.Target.AssertSome();
      var elements = simulation.SimConfig.SimOutput.SimValues.Bind(v => v.SimElements);
      var outputState = _appState.SimSharedState.ElementSharedStates.Map(
        ess => elements.Find(e => e.Name == ess.Name).AssertSome()
        );

      ActiveSharedStateProvider.ApplyState(
        SimSharedStateApply.Outputs | SimSharedStateApply.Set,
        default,
        outputState,
        default
        );
    }

    private void HandleShareOutputsState() => HandleShareState(SimSharedStateBuild.Outputs);

    private void HandleApplyObservationsState()
    {
      RequireNotNull(ActiveSharedStateProvider);

      var observationsState = _appState.SimSharedState.ObservationsSharedStates
        .Map(oss => _appState.SimEvidence.GetObservations(oss.Reference))
        .Somes()
        .ToArr();

      ActiveSharedStateProvider.ApplyState(
        SimSharedStateApply.Observations | SimSharedStateApply.Set,
        default,
        default,
        observationsState
        );
    }

    private void HandleShareObservationsState() => HandleShareState(SimSharedStateBuild.Observations);

    private void HandleApplyState()
    {
      RequireNotNull(ActiveSharedStateProvider);

      var simulation = _appState.Target.AssertSome();

      var input = simulation.SimConfig.SimInput;
      var parameterState = _appState.SimSharedState.ParameterSharedStates.Map(
        pss => (
          input.SimParameters.GetParameter(pss.Name).With(pss.Value),
          pss.Minimum,
          pss.Maximum,
          pss.Distribution
          )
        );

      var elements = simulation.SimConfig.SimOutput.SimValues.Bind(v => v.SimElements);
      var outputState = _appState.SimSharedState.ElementSharedStates.Map(
        ess => elements.Find(e => e.Name == ess.Name).AssertSome()
        );

      var observationsState = _appState.SimSharedState.ObservationsSharedStates
        .Map(oss => _appState.SimEvidence.GetObservations(oss.Reference))
        .Somes()
        .ToArr();

      ActiveSharedStateProvider.ApplyState(
        SimSharedStateApply.All | SimSharedStateApply.Set,
        parameterState,
        outputState,
        observationsState
        );
    }

    private void HandleShareState() => HandleShareState(SimSharedStateBuild.All);

    private void HandleShareState(SimSharedStateBuild buildType)
    {
      RequireNotNull(ActiveSharedStateProvider);

      using var builder = new SimSharedStateBuilder(
        _appState.SimSharedState,
        _appState.Target.AssertSome(),
        _appState.SimEvidence,
        buildType
        );
      ActiveSharedStateProvider.ShareState(builder);
    }

    private void HandleApplySingleParameterState(SimParameterSharedState parameterSharedState)
    {
      RequireNotNull(ActiveSharedStateProvider);

      var simulation = _appState.Target.AssertSome();
      var input = simulation.SimConfig.SimInput;
      var parameter = input.SimParameters
        .GetParameter(parameterSharedState.Name)
        .With(parameterSharedState.Value);
      var parameterState = (
        parameter,
        parameterSharedState.Minimum,
        parameterSharedState.Maximum,
        parameterSharedState.Distribution
        );

      ActiveSharedStateProvider.ApplyState(
        SimSharedStateApply.Parameters | SimSharedStateApply.Single,
        Prelude.Array(parameterState),
        default,
        default
        );
    }

    private void HandleApplySingleOutputState(SimElementSharedState elementSharedState)
    {
      RequireNotNull(ActiveSharedStateProvider);

      var simulation = _appState.Target.AssertSome();
      var elements = simulation.SimConfig.SimOutput.SimValues.Bind(v => v.SimElements);
      var outputState = elements.Find(e => e.Name == elementSharedState.Name).AssertSome();

      ActiveSharedStateProvider.ApplyState(
        SimSharedStateApply.Outputs | SimSharedStateApply.Single,
        default,
        Prelude.Array(outputState),
        default
        );
    }

    private void HandleApplySingleObservationsState(SimObservations observations)
    {
      RequireNotNull(ActiveSharedStateProvider);

      ActiveSharedStateProvider.ApplyState(
        SimSharedStateApply.Observations | SimSharedStateApply.Single,
        default,
        default,
        Prelude.Array(observations)
        );
    }

    private void HandleCloseView()
    {
      IsViewOpen = false;
    }

    private void ObserveActiveUIComponent(object _)
    {
      UpdateSharedStateTarget();
    }

    private void ObserveParameterSharedStates(object _)
    {
      UpdateSharedParameters();
    }

    private void ObserveElementSharedStates(object _)
    {
      UpdateSharedOutputs();
    }

    private void ObserveObservationsSharedStates(object _)
    {
      UpdateSharedObservations();
    }

    private void UpdateSharedStateTarget()
    {
      var (_, _, _, _, viewModel) = _appState.ActiveUIComponent;

      ActiveSharedStateProvider = viewModel as ISharedStateProvider;
    }

    private void UpdateSharedParameters()
    {
      object?[][] SomeSimulation(Simulation simulation)
      {
        var parameters = simulation.SimConfig.SimInput.SimParameters;
        return _appState.SimSharedState.ParameterSharedStates
          .Map(pss => new object?[]
          {
            _applySingleParameterState,
            pss,
            pss.Name,
            $"{pss.Minimum:G4} < {pss.Value:G4} < {pss.Maximum:G4}",
            parameters.GetParameter(pss.Name).Unit,
            pss.Distribution.ToStringIfSome(pss.Name)
          })
          .ToArray();
      }

      object[][] NoSimulation() => Array.Empty<object[]>();

      SharedParameters = _appState.Target.Match(SomeSimulation, NoSimulation);
    }

    private void UpdateSharedOutputs()
    {
      var sharedOutputs = _appState.SimSharedState.ElementSharedStates
        .OrderBy(ess => ess.Name.ToUpperInvariant())
        .Select(ess => new object[] { _applySingleOutputState, ess, ess.Name })
        .ToArray();
      SharedOutputs = sharedOutputs;
    }

    private void UpdateSharedObservations()
    {
      SharedObservations = _appState.SimSharedState.ObservationsSharedStates
        .Map(oss =>
          from o in _appState.SimEvidence.GetObservations(oss.Reference)
          from es in _appState.SimEvidence.EvidenceSources.Find(es => es.ID == o.EvidenceSourceID)
          select new object[]
          {
            _applySingleObservationsState,
            o,
            o.GetFQObservationsName(es)
          }
        )
        .Somes()
        .ToArray();
    }

    private readonly IAppState _appState;
    private readonly ICommand _applySingleParameterState;
    private readonly ICommand _applySingleOutputState;
    private readonly ICommand _applySingleObservationsState;
  }
}
