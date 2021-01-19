using LanguageExt;
using ReactiveUI;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Double;
using static RVisUI.Wpf.WpfTools;

namespace Estimation
{
  [DisplayName("Heteroscedastic (power)")]
  internal sealed class HeteroscedasticPowerErrorViewModel : IHeteroscedasticPowerErrorViewModel, INotifyPropertyChanged
  {
    internal HeteroscedasticPowerErrorViewModel(IAppService appService)
    {
      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

      this
        .WhenAny(vm => vm.ErrorModel, _ => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveErrorModel
            )
          ),

      this
        .WhenAny(
          vm => vm.Delta1,
          vm => vm.Delta1StepInitializer,
          vm => vm.Delta2,
          vm => vm.Delta2StepInitializer,
          vm => vm.Sigma,
          vm => vm.SigmaStepInitializer,
          vm => vm.Lower,
          (_, __, ___, ____, _____, ______, ________) => default(object)
        )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveErrorModelParameters
            )
          )

        );
    }

    public HeteroscedasticPowerErrorViewModel()
      : this(new RVisUI.AppInf.Design.AppService())
    {
      RequireTrue(IsInDesignMode);

      ErrorModel = HeteroscedasticPowerErrorModel.Default;
    }

    public ErrorModelType ErrorModelType => ErrorModelType.HeteroscedasticPower;

    public double? Delta1
    {
      get => _delta1;
      set => this.RaiseAndSetIfChanged(ref _delta1, value, PropertyChanged);
    }
    private double? _delta1;

    public double? Delta1StepInitializer
    {
      get => _delta1StepInitializer;
      set => this.RaiseAndSetIfChanged(ref _delta1StepInitializer, value, PropertyChanged);
    }
    private double? _delta1StepInitializer;

    public double? Delta2
    {
      get => _delta2;
      set => this.RaiseAndSetIfChanged(ref _delta2, value, PropertyChanged);
    }
    private double? _delta2;

    public double? Delta2StepInitializer
    {
      get => _delta2StepInitializer;
      set => this.RaiseAndSetIfChanged(ref _delta2StepInitializer, value, PropertyChanged);
    }
    private double? _delta2StepInitializer;

    public double? Sigma
    {
      get => _sigma;
      set => this.RaiseAndSetIfChanged(ref _sigma, value, PropertyChanged);
    }
    private double? _sigma;

    public double? SigmaStepInitializer
    {
      get => _sigmaStepInitializer;
      set => this.RaiseAndSetIfChanged(ref _sigmaStepInitializer, value, PropertyChanged);
    }
    private double? _sigmaStepInitializer;

    public double? Var
    {
      get => _var;
      private set => this.RaiseAndSetIfChanged(ref _var, value, PropertyChanged);
    }
    private double? _var;

    public double? Lower
    {
      get => _lower;
      set => this.RaiseAndSetIfChanged(ref _lower, value, PropertyChanged);
    }
    private double? _lower;

    public Option<HeteroscedasticPowerErrorModel> ErrorModel
    {
      get => _errorModel;
      set => this.RaiseAndSetIfChanged(ref _errorModel, value, PropertyChanged);
    }
    private Option<HeteroscedasticPowerErrorModel> _errorModel;

    public IErrorModel? ErrorModelUnsafe
    {
      get => _errorModel.Match(em => em, () => default(IErrorModel));
      set => ErrorModel = value == default ? None : Some(RequireInstanceOf<HeteroscedasticPowerErrorModel>(value));
    }

    public string? Variable
    {
      get => _variable;
      set => this.RaiseAndSetIfChanged(ref _variable, value, PropertyChanged);
    }
    private string? _variable;

    public string? Unit
    {
      get => _unit;
      set => this.RaiseAndSetIfChanged(ref _unit, value, PropertyChanged);
    }
    private string? _unit;

    public void Dispose() => Dispose(true);

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private void ObserveErrorModel(object? _)
    {
      ErrorModel.Match(
        errorModel =>
        {
          Delta1 = IsNaN(errorModel.Delta1) ? default(double?) : errorModel.Delta1;
          Delta1StepInitializer = IsNaN(errorModel.Delta1StepInitializer) ? default(double?) : errorModel.Delta1StepInitializer;
          Delta2 = IsNaN(errorModel.Delta2) ? default(double?) : errorModel.Delta2;
          Delta2StepInitializer = IsNaN(errorModel.Delta2StepInitializer) ? default(double?) : errorModel.Delta2StepInitializer;
          Sigma = IsNaN(errorModel.Sigma) ? default(double?) : errorModel.Sigma;
          SigmaStepInitializer = IsNaN(errorModel.SigmaStepInitializer) ? default(double?) : errorModel.SigmaStepInitializer;
          Lower = IsNaN(errorModel.Lower) ? default(double?) : errorModel.Lower;
          
          Var = Sigma * Sigma;
        },
        () =>
        {
          Delta1 = default;
          Delta1StepInitializer = default;
          Delta2 = default;
          Delta2StepInitializer = default;
          Sigma = default;
          SigmaStepInitializer = default;
          Lower = default;

          Var = default;
        });
    }

    private void ObserveErrorModelParameters(object? _)
    {
      if (IsConfigured)
      {
        ErrorModel = new HeteroscedasticPowerErrorModel(
          Delta1!.Value,
          Delta1StepInitializer!.Value,
          Delta2!.Value,
          Delta2StepInitializer!.Value,
          Sigma!.Value,
          SigmaStepInitializer!.Value,
          Lower!.Value
          );
      }

      Var = Sigma * Sigma;
    }

    private bool IsConfigured =>
      Delta1.HasValue &&
      Delta1StepInitializer.HasValue &&
      Delta2.HasValue &&
      Delta2StepInitializer.HasValue &&
      Sigma.HasValue &&
      SigmaStepInitializer.HasValue &&
      Lower.HasValue;

    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
