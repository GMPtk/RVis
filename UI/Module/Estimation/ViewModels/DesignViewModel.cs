using LanguageExt;
using ReactiveUI;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
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
using static System.Math;

namespace Estimation
{
  internal sealed class DesignViewModel : IDesignViewModel, INotifyPropertyChanged, IDisposable
  {
    internal DesignViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      ModuleState moduleState,
      EstimationDesigns estimationDesigns
      )
    {
      _appState = appState;
      _appService = appService;
      _appSettings = appSettings;
      _moduleState = moduleState;
      _estimationDesigns = estimationDesigns;

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        if (_moduleState.EstimationDesign == default)
        {
          Populate();
        }
        else
        {
          Populate(_moduleState.EstimationDesign);
        }

        UpdateDesignEnable();
      }

      CreateDesign = ReactiveCommand.Create(
        HandleCreateDesign,
        this.WhenAny(
          vm => vm.CanCreateDesign,
          vm => vm.Iterations,
          vm => vm.BurnIn,
          (_, __, ___) =>
            CanCreateDesign &&
            BurnIn >= 0 &&
            BurnIn < Iterations
          )
        );

      UnloadDesign = ReactiveCommand.Create(
        HandleUnloadDesign,
        this.WhenAny(vm => vm.CanUnloadDesign, _ => CanUnloadDesign)
        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _subscriptions = new CompositeDisposable(

          moduleState
            .WhenAnyValue(
              ms => ms.PriorStates,
              ms => ms.OutputStates,
              ms => ms.SelectedObservations
              )
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<(Arr<ParameterState>, Arr<OutputState>, Arr<SimObservations>)>(
                ObserveEstimationStateChange
                )
              ),

          moduleState
            .ObservableForProperty(vm => vm.EstimationDesign)
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<object>(
                ObserveModuleStateEstimationDesign
                )
              ),

          _appSettings
            .ObservableForProperty(@as => @as.RThrottlingUseCores)
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<object>(
                ObserveAppSettingsRThrottlingUseCores
                )
              ),

          this
            .WhenAnyValue(vm => vm.Iterations, vm => vm.BurnIn, vm => vm.ChainsIndex)
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<(int?, int?, int)>(
                ObserveInputs
                )
            )

          );
      }
    }

    public Arr<string> Priors
    {
      get => _priors;
      set => this.RaiseAndSetIfChanged(ref _priors, value, PropertyChanged);
    }
    private Arr<string> _priors;

    public Arr<string> Invariants
    {
      get => _invariants;
      set => this.RaiseAndSetIfChanged(ref _invariants, value, PropertyChanged);
    }
    private Arr<string> _invariants;

    public Arr<string> Outputs
    {
      get => _outputs;
      set => this.RaiseAndSetIfChanged(ref _outputs, value, PropertyChanged);
    }
    private Arr<string> _outputs;

    public Arr<string> Observations
    {
      get => _observations;
      set => this.RaiseAndSetIfChanged(ref _observations, value, PropertyChanged);
    }
    private Arr<string> _observations;

    public int? Iterations
    {
      get => _iterations;
      set => this.RaiseAndSetIfChanged(ref _iterations, value, PropertyChanged);
    }
    private int? _iterations;

    public int? BurnIn
    {
      get => _burnIn;
      set => this.RaiseAndSetIfChanged(ref _burnIn, value, PropertyChanged);
    }
    private int? _burnIn;

    public Arr<int> ChainsOptions
    {
      get => _chainsOptions;
      set => this.RaiseAndSetIfChanged(ref _chainsOptions, value, PropertyChanged);
    }
    private Arr<int> _chainsOptions;

    public int ChainsIndex
    {
      get => _chainsIndex;
      set => this.RaiseAndSetIfChanged(ref _chainsIndex, value, PropertyChanged);
    }
    private int _chainsIndex;

    public ICommand CreateDesign { get; }

    public bool CanCreateDesign
    {
      get => _canCreateDesign;
      set => this.RaiseAndSetIfChanged(ref _canCreateDesign, value, PropertyChanged);
    }
    private bool _canCreateDesign;

    public DateTime? DesignCreatedOn
    {
      get => _designCreatedOn;
      set => this.RaiseAndSetIfChanged(ref _designCreatedOn, value, PropertyChanged);
    }
    private DateTime? _designCreatedOn;

    public ICommand UnloadDesign { get; }

    public bool CanUnloadDesign
    {
      get => _canUnloadDesign;
      set => this.RaiseAndSetIfChanged(ref _canUnloadDesign, value, PropertyChanged);
    }
    private bool _canUnloadDesign;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
        }

        _disposed = true;
      }
    }

    private void UpdateDesignEnable()
    {
      CanCreateDesign = _moduleState.EstimationDesign == default;
      CanUnloadDesign = _moduleState.EstimationDesign != default;
    }

    private void HandleCreateDesign()
    {
      RequireTrue(BurnIn >= 0 && BurnIn < Iterations);

      try
      {
        RequireFalse(_moduleState.PriorStates.IsEmpty, "No priors");
        RequireFalse(_moduleState.OutputStates.IsEmpty, "No outputs");
        RequireFalse(_moduleState.SelectedObservations.IsEmpty, "No observations");

        var priorDistributions = _moduleState.PriorStates
          .Filter(ps => ps.IsSelected)
          .OrderBy(ps => ps.Name.ToUpperInvariant())
          .Select(ps => (ps.Name, Distribution: ps.GetDistribution()))
          .ToArr();

        RequireTrue(
          priorDistributions
            .Filter(pd => pd.Distribution.DistributionType != DistributionType.Invariant)
            .ForAll(t => t.Distribution.IsConfigured),
          "One or more priors not configured"
          );

        RequireTrue(
          priorDistributions
            .Filter(pd => pd.Distribution.DistributionType == DistributionType.Invariant)
            .ForAll(t => t.Distribution.IsConfigured),
          "One or more invariant parameters not configured"
          );

        var outputErrorModels = _moduleState.OutputStates
          .Filter(os => os.IsSelected)
          .OrderBy(os => os.Name.ToUpperInvariant())
          .Select(os => (os.Name, ErrorModel: os.GetErrorModel()))
          .ToArr();

        RequireTrue(
          outputErrorModels.ForAll(t => t.ErrorModel.IsConfigured),
          "One or more output error models not configured"
          );

        var observations = _moduleState.GetRelevantObservations();

        RequireTrue(
          outputErrorModels.ForAll(oem => observations.Exists(o => o.Subject == oem.Name)),
          "Likelihood requires all outputs to have one or more observations sets"
          );

        var estimationDesign = EstimationDesign.CreateEstimationDesign(
          DateTime.Now,
          priorDistributions,
          outputErrorModels,
          observations,
          Iterations.Value,
          BurnIn.Value,
          ChainsOptions[ChainsIndex]
          );

        using (_reactiveSafeInvoke.SuspendedReactivity)
        {
          _estimationDesigns.Add(estimationDesign);
          _moduleState.PosteriorState = default;
          _moduleState.ChainStates = default;
          _moduleState.EstimationDesign = estimationDesign;
          DesignCreatedOn = estimationDesign.CreatedOn;
          UpdateDesignEnable();
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
    }

    private void HandleUnloadDesign()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _moduleState.PosteriorState = default;
        _moduleState.ChainStates = default;
        _moduleState.EstimationDesign = default;
        DesignCreatedOn = default;
        UpdateDesignEnable();
      }
    }

    private void ObserveEstimationStateChange((Arr<ParameterState>, Arr<OutputState>, Arr<SimObservations>) _)
    {
      _moduleState.PosteriorState = default;
      _moduleState.ChainStates = default;
      _moduleState.EstimationDesign = default;
      PopulatePriors(_moduleState.PriorStates);
      PopulateInvariants(_moduleState.PriorStates);
      PopulateOutputs(_moduleState.OutputStates);
      PopulateObservations(_moduleState.GetRelevantObservations());
      DesignCreatedOn = default;
      UpdateDesignEnable();
    }

    private void ObserveModuleStateEstimationDesign(object _)
    {
      if (_moduleState.EstimationDesign == default)
      {
        Populate();
      }
      else
      {
        Populate(_moduleState.EstimationDesign);
      }

      UpdateDesignEnable();
    }

    private void ObserveAppSettingsRThrottlingUseCores(object _)
    {
      PopulateChains(_appSettings.RThrottlingUseCores, _moduleState.DesignState.Chains);
      _moduleState.DesignState.Chains = ChainsOptions[ChainsIndex];
    }

    private void ObserveInputs((int?, int?, int) _)
    {
      _moduleState.DesignState.Iterations = Iterations;
      _moduleState.DesignState.BurnIn = BurnIn;
      _moduleState.DesignState.Chains = ChainsOptions[ChainsIndex];

      UpdateDesignEnable();
    }

    private void Populate()
    {
      PopulatePriors(_moduleState.PriorStates);
      PopulateInvariants(_moduleState.PriorStates);
      PopulateOutputs(_moduleState.OutputStates);
      PopulateObservations(_moduleState.GetRelevantObservations());

      Iterations = _moduleState.DesignState.Iterations ?? DEFAULT_ITERATIONS;
      BurnIn = _moduleState.DesignState.BurnIn ?? DEFAULT_BURNIN;

      PopulateChains(_appSettings.RThrottlingUseCores, _moduleState.DesignState.Chains);

      DesignCreatedOn = default;
    }

    private void Populate(EstimationDesign estimationDesign)
    {
      PopulatePriors(estimationDesign.Priors);
      PopulateInvariants(estimationDesign.Priors);
      PopulateOutputs(estimationDesign.Outputs);
      PopulateObservations(estimationDesign.Observations);

      Iterations = estimationDesign.Iterations;
      BurnIn = estimationDesign.BurnIn;

      PopulateChains(_appSettings.RThrottlingUseCores, estimationDesign.Chains);

      DesignCreatedOn = estimationDesign.CreatedOn;
    }

    private void PopulatePriors(Arr<ParameterState> priorStates) =>
      Priors = priorStates
        .Filter(ps => ps.IsSelected)
        .Map(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .Filter(t => t.Distribution.DistributionType != DistributionType.Invariant)
        .OrderBy(t => t.Name.ToUpperInvariant())
        .Select(t => t.Distribution.ToString(t.Name))
        .ToArr();

    private void PopulatePriors(Arr<ModelParameter> priors) =>
      Priors = priors
        .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
        .OrderBy(dp => dp.Name.ToUpperInvariant())
        .Select(dp => dp.Distribution.ToString(dp.Name))
        .ToArr();

    private void PopulateInvariants(Arr<ParameterState> priorStates) =>
      Invariants = priorStates
        .Filter(ps => ps.IsSelected)
        .Map(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .Filter(t => t.Distribution.DistributionType == DistributionType.Invariant)
        .OrderBy(t => t.Name.ToUpperInvariant())
        .Select(t => t.Distribution.ToString(t.Name))
        .ToArr();

    private void PopulateInvariants(Arr<ModelParameter> priors) =>
      Invariants = priors
        .Filter(dp => dp.Distribution.DistributionType == DistributionType.Invariant)
        .OrderBy(dp => dp.Name.ToUpperInvariant())
        .Select(dp => dp.Distribution.ToString(dp.Name))
        .ToArr();

    private void PopulateOutputs(Arr<OutputState> outputStates) =>
      Outputs = outputStates
        .Filter(ps => ps.IsSelected)
        .Map(ps => (ps.Name, ErrorModel: ps.GetErrorModel()))
        .OrderBy(t => t.Name.ToUpperInvariant())
        .Select(t => t.ErrorModel.ToString(t.Name))
        .ToArr();

    private void PopulateOutputs(Arr<ModelOutput> outputs) =>
      Outputs = outputs
        .OrderBy(dp => dp.Name.ToUpperInvariant())
        .Select(dp => dp.ErrorModel.ToString(dp.Name))
        .ToArr();

    private void PopulateObservations(Arr<SimObservations> selectedObservations) =>
      Observations = selectedObservations
        .OrderBy(o => o.Subject.ToUpperInvariant())
        .Select(o => _appState.SimEvidence.GetFQObservationsName(o))
        .ToArr();

    private void PopulateChains(int maxCores, int? useCores)
    {
      RequireTrue(maxCores > 0);

      var chains = useCores ?? DEFAULT_CHAINS;
      chains = Min(chains, maxCores);

      ChainsOptions = Range(1, maxCores).ToArr();
      ChainsIndex = ChainsOptions.IndexOf(chains);
    }

    private const int DEFAULT_ITERATIONS = 1000;
    private const int DEFAULT_BURNIN = 50;
    private const int DEFAULT_CHAINS = 4;

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly EstimationDesigns _estimationDesigns;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
