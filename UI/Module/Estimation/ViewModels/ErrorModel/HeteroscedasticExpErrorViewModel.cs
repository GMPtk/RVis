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
  [DisplayName("Heteroscedastic (exponential)")]
  internal sealed class HeteroscedasticExpErrorViewModel : IHeteroscedasticExpErrorViewModel, INotifyPropertyChanged
  {
    internal HeteroscedasticExpErrorViewModel(IAppService appService)
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
          vm => vm.Delta, 
          vm => vm.DeltaStepInitializer, 
          vm => vm.Sigma, 
          vm => vm.SigmaStepInitializer, 
          vm => vm.Lower, 
          (_,__,___, ____, _____) => default(object)
          )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveErrorModelParameters
            )
          )

        );
    }

    public HeteroscedasticExpErrorViewModel()
      : this(new RVisUI.AppInf.Design.AppService())
    {
      RequireTrue(IsInDesignMode);

      ErrorModel = HeteroscedasticExpErrorModel.Default;
    }

    public ErrorModelType ErrorModelType => ErrorModelType.HeteroscedasticExp;

    public double? Delta
    {
      get => _delta;
      set => this.RaiseAndSetIfChanged(ref _delta, value, PropertyChanged);
    }
    private double? _delta;

    public double? DeltaStepInitializer
    {
      get => _deltaStepInitializer;
      set => this.RaiseAndSetIfChanged(ref _deltaStepInitializer, value, PropertyChanged);
    }
    private double? _deltaStepInitializer;

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

    public Option<HeteroscedasticExpErrorModel> ErrorModel
    {
      get => _errorModel;
      set => this.RaiseAndSetIfChanged(ref _errorModel, value, PropertyChanged);
    }
    private Option<HeteroscedasticExpErrorModel> _errorModel;

    public IErrorModel? ErrorModelUnsafe
    {
      get => _errorModel.Match(em => em, () => default(IErrorModel));
      set => ErrorModel = value == default ? None : Some(RequireInstanceOf<HeteroscedasticExpErrorModel>(value));
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
          Delta = IsNaN(errorModel.Delta) ? default(double?) : errorModel.Delta;
          DeltaStepInitializer = IsNaN(errorModel.DeltaStepInitializer) ? default(double?) : errorModel.DeltaStepInitializer;
          Sigma = IsNaN(errorModel.Sigma) ? default(double?) : errorModel.Sigma;
          SigmaStepInitializer = IsNaN(errorModel.SigmaStepInitializer) ? default(double?) : errorModel.SigmaStepInitializer;
          Lower = IsNaN(errorModel.Lower) ? default(double?) : errorModel.Lower;
          
          Var = Sigma * Sigma;
        },
        () =>
        {
          Delta = default;
          DeltaStepInitializer = default;
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
        ErrorModel = new HeteroscedasticExpErrorModel(
          Delta!.Value,
          DeltaStepInitializer!.Value,
          Sigma!.Value,
          SigmaStepInitializer!.Value,
          Lower!.Value
          );
      }

      Var = Sigma * Sigma;
    }

    private bool IsConfigured =>
      Delta.HasValue &&
      DeltaStepInitializer.HasValue &&
      Sigma.HasValue &&
      SigmaStepInitializer.HasValue &&
      Lower.HasValue;

    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
