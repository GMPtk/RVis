using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Input;
using static RVis.Base.Check;
using static System.Double;

namespace Sampling
{
  internal sealed class LHSConfigurationViewModel : ILHSConfigurationViewModel, INotifyPropertyChanged
  {
    internal LHSConfigurationViewModel(IAppState appState, IAppService appService)
    {
      Cancel = ReactiveCommand.Create(HandleCancel);

      IsDiceDesignInstalled = appState.InstalledRPackages.Exists(p => p.Package == "DiceDesign");

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
          _reactiveSafeInvoke.SuspendAndInvoke<(LatinHypercubeDesignType, bool, double?, double?)>(
            ObserveInputs1
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
          _reactiveSafeInvoke.SuspendAndInvoke<(int?, double?, TemperatureDownProfile, int?)>(
            ObserveInputs2
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

    public LatinHypercubeDesign LatinHypercubeDesign
    {
      get => _latinHypercubeDesign;
      set => this.RaiseAndSetIfChanged(ref _latinHypercubeDesign, value, PropertyChanged);
    }
    private LatinHypercubeDesign _latinHypercubeDesign;

    public event PropertyChangedEventHandler? PropertyChanged;

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
        var latinHypercubeDesignType = LatinHypercubeDesignType;
        var t0 = UseSimulatedAnnealing ? T0!.Value : NaN;
        var c = UseSimulatedAnnealing ? C!.Value : NaN;
        var iterations = UseSimulatedAnnealing ? Iterations!.Value : default;
        var p = UseSimulatedAnnealing ? P!.Value : NaN;
        var profile = Profile;
        var imax = UseSimulatedAnnealing ? Imax!.Value : default;

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

    private void ObserveInputs1((LatinHypercubeDesignType, bool, double?, double?) _)
    {
      UpdateEnable();
    }

    private void ObserveInputs2((int?, double?, TemperatureDownProfile, int?) _)
    {
      UpdateEnable();
    }

    private void ObserveLatinHypercubeDesign(object _)
    {
      Populate(LatinHypercubeDesign);
      UpdateEnable();
    }

    private void UpdateEnable()
    {
      var haveDesignType = LatinHypercubeDesignType != LatinHypercubeDesignType.None;

      var hasSAConfiguration =
           !UseSimulatedAnnealing
        || (T0.HasValue && C.HasValue && Iterations.HasValue && P.HasValue);

      var hasImax =
           !UseSimulatedAnnealing
        || Profile != TemperatureDownProfile.GeometricalMorris
        || Imax.HasValue;

      CanOK =
        haveDesignType &&
        hasSAConfiguration &&
        hasImax;
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
  }
}
