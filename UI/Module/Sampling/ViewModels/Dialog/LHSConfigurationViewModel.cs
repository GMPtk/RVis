using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using static System.Double;

namespace Sampling
{
  internal class LHSConfigurationViewModel : ILHSConfigurationViewModel, INotifyPropertyChanged
  {
    internal LHSConfigurationViewModel(IAppState appState, IAppService appService)
    {
      Cancel = ReactiveCommand.Create(HandleCancel);

      IsDiceDesignInstalled = appState.InstalledRPackages.Exists(p => p.Package == "DiceDesign");

      if (!IsDiceDesignInstalled) return;

      Disable = ReactiveCommand.Create(HandleDisable);

      OK = ReactiveCommand.Create(
        HandleOK,
        this.ObservableForProperty(vm => vm.CanOK, _ => CanOK)
        );

      UpdateEnable();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      this
        .WhenAnyValue(
          vm => vm.LatinHypercubeDesignType,
          vm => vm.UseSimulatedAnnealing,
          vm => vm.T0,
          vm => vm.C
        )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveInputs
          )
        );

      this
        .WhenAnyValue(
          vm => vm.Iterations,
          vm => vm.P,
          vm => vm.Profile,
          vm => vm.Imax
        )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveInputs
          )
        );

      this
        .ObservableForProperty(
          vm => vm.Variables
          )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveVariables
          )
        );

      this
        .ObservableForProperty(
          vm => vm.LatinHypercubeDesign
          )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveLatinHypercubeDesign
          )
        );
    }

    public bool IsDiceDesignInstalled { get; }

    public Arr<ILHSParameterViewModel> LHSParameterViewModels
    {
      get => _lhsParameterViewModels;
      set => this.RaiseAndSetIfChanged(ref _lhsParameterViewModels, value, PropertyChanged);
    }
    private Arr<ILHSParameterViewModel> _lhsParameterViewModels;

    public LatinHypercubeDesignType LatinHypercubeDesignType
    {
      get => _latinHypercubeDesignType;
      set => this.RaiseAndSetIfChanged(ref _latinHypercubeDesignType, value, PropertyChanged);
    }
    private LatinHypercubeDesignType _latinHypercubeDesignType;

    public bool UseSimulatedAnnealing
    {
      get => _useSimulatedAnnealing;
      set => this.RaiseAndSetIfChanged(ref _useSimulatedAnnealing, value, PropertyChanged);
    }
    private bool _useSimulatedAnnealing;

    public double? T0
    {
      get => _t0;
      set => this.RaiseAndSetIfChanged(ref _t0, value, PropertyChanged);
    }
    private double? _t0;

    public double? C
    {
      get => _c;
      set => this.RaiseAndSetIfChanged(ref _c, value, PropertyChanged);
    }
    private double? _c;

    public int? Iterations
    {
      get => _iterations;
      set => this.RaiseAndSetIfChanged(ref _iterations, value, PropertyChanged);
    }
    private int? _iterations;

    public double? P
    {
      get => _p;
      set => this.RaiseAndSetIfChanged(ref _p, value, PropertyChanged);
    }
    private double? _p;

    public TemperatureDownProfile Profile
    {
      get => _profile;
      set => this.RaiseAndSetIfChanged(ref _profile, value, PropertyChanged);
    }
    private TemperatureDownProfile _profile;

    public int? Imax
    {
      get => _imax;
      set => this.RaiseAndSetIfChanged(ref _imax, value, PropertyChanged);
    }
    private int? _imax;

    public ICommand Disable { get; }

    public bool CanOK
    {
      get => _canOK;
      set => this.RaiseAndSetIfChanged(ref _canOK, value, PropertyChanged);
    }
    private bool _canOK;

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value, PropertyChanged);
    }
    private bool? _dialogResult;

    public Arr<(string Parameter, double Lower, double Upper)> Variables
    {
      get => _variables;
      set => this.RaiseAndSetIfChanged(ref _variables, value, PropertyChanged);
    }
    private Arr<(string Parameter, double Lower, double Upper)> _variables;

    public LatinHypercubeDesign LatinHypercubeDesign
    {
      get => _latinHypercubeDesign;
      set => this.RaiseAndSetIfChanged(ref _latinHypercubeDesign, value, PropertyChanged);
    }
    private LatinHypercubeDesign _latinHypercubeDesign;

    public event PropertyChangedEventHandler PropertyChanged;

    private void HandleDisable()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        LatinHypercubeDesignType = LatinHypercubeDesignType.None;
      }

      HandleOK();
    }

    private void HandleOK()
    {
      if (LatinHypercubeDesignType != LatinHypercubeDesignType.None)
      {
        var variables = LHSParameterViewModels
          .Map(vm => (vm.Name, vm.Lower.Value, vm.Upper.Value))
          .ToArr();

        var latinHypercubeDesignType = LatinHypercubeDesignType;
        var t0 = UseSimulatedAnnealing ? T0.Value : NaN;
        var c = UseSimulatedAnnealing ? C.Value : NaN;
        var iterations = UseSimulatedAnnealing ? Iterations.Value : default;
        var p = UseSimulatedAnnealing ? P.Value : NaN;
        var profile = Profile;
        var imax = UseSimulatedAnnealing ? Imax.Value : default;

        var latinHypercubeDesign = new LatinHypercubeDesign(
          latinHypercubeDesignType,
          t0,
          c,
          iterations,
          p,
          profile,
          imax
          );

        using (_reactiveSafeInvoke.SuspendedReactivity)
        {
          Variables = variables;
          LatinHypercubeDesign = latinHypercubeDesign;
        }
      }
      else
      {
        LatinHypercubeDesign = LatinHypercubeDesign.Default;
      }

      DialogResult = true;
    }

    private void HandleCancel() => DialogResult = false;

    private void ObserveInputs(object _)
    {
      UpdateEnable();
    }

    private void ObserveVariables(object _)
    {
      Populate(Variables);
      UpdateEnable();
    }

    private void ObserveLatinHypercubeDesign(object _)
    {
      Populate(LatinHypercubeDesign);
      UpdateEnable();
    }

    private void ObserveParameterViewModel(object _)
    {
      UpdateEnable();
    }

    private void UpdateEnable()
    {
      var haveDesignType = LatinHypercubeDesignType != LatinHypercubeDesignType.None;

      var areVariablesPopulated = LHSParameterViewModels.ForAll(vm => vm.Upper >= vm.Lower);

      var hasSAConfiguration =
           !UseSimulatedAnnealing
        || (T0.HasValue && C.HasValue && Iterations.HasValue && P.HasValue);

      var hasImax =
           !UseSimulatedAnnealing
        || Profile != TemperatureDownProfile.GeometricalMorris
        || Imax.HasValue;

      CanOK = 
        haveDesignType && 
        areVariablesPopulated && 
        hasSAConfiguration && 
        hasImax;
    }

    private void Populate(Arr<(string Parameter, double Lower, double Upper)> variables)
    {
      _parameterViewModelSubscriptions?.Dispose();

      LHSParameterViewModels = variables
        .Map(v => new LHSParameterViewModel(v.Parameter) { Lower = v.Lower, Upper = v.Upper })
        .ToArr<ILHSParameterViewModel>();

      var subscriptions = LHSParameterViewModels
        .Map(pvm => pvm
          .WhenAnyValue(vm => vm.Lower, vm => vm.Upper)
          .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveParameterViewModel))
          );

      _parameterViewModelSubscriptions = new CompositeDisposable(subscriptions.ToArray());
    }

    private void Populate(LatinHypercubeDesign latinHypercubeDesign)
    {
      LatinHypercubeDesignType = latinHypercubeDesign.LatinHypercubeDesignType;

      T0 = latinHypercubeDesign.T0.ToNullable();
      C = latinHypercubeDesign.C.ToNullable();
      Iterations = latinHypercubeDesign.Iterations;
      P = latinHypercubeDesign.P.ToNullable();
      Profile = latinHypercubeDesign.Profile;
      Imax = latinHypercubeDesign.Imax;

      UseSimulatedAnnealing = !IsNaN(latinHypercubeDesign.T0);

      if (!UseSimulatedAnnealing)
      {
        T0 = LatinHypercubeDesign.Default.T0;
        C = LatinHypercubeDesign.Default.C;
        Iterations = LatinHypercubeDesign.Default.Iterations;
        P = LatinHypercubeDesign.Default.P;
        Profile = LatinHypercubeDesign.Default.Profile;
        Imax = LatinHypercubeDesign.Default.Imax;
      }
    }

    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private IDisposable _parameterViewModelSubscriptions;
  }
}
