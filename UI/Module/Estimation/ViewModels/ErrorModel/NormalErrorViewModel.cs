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
  [DisplayName("Normal")]
  internal sealed class NormalErrorViewModel : INormalErrorViewModel, INotifyPropertyChanged
  {
    internal NormalErrorViewModel(IAppService appService)
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
        .WhenAny(vm => vm.Sigma, vm => vm.StepInitializer, (_, __) => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveErrorModelParameters
            )
          )

        );
    }

    public NormalErrorViewModel()
      : this(new RVisUI.AppInf.Design.AppService())
    {
      RequireTrue(IsInDesignMode);
      ErrorModel = NormalErrorModel.Default;
    }

    public ErrorModelType ErrorModelType => ErrorModelType.Normal;

    public double? Sigma
    {
      get => _sigma;
      set => this.RaiseAndSetIfChanged(ref _sigma, value, PropertyChanged);
    }
    private double? _sigma;

    public double? StepInitializer
    {
      get => _stepInitializer;
      set => this.RaiseAndSetIfChanged(ref _stepInitializer, value, PropertyChanged);
    }
    private double? _stepInitializer;

    public double? Var
    {
      get => _var;
      private set => this.RaiseAndSetIfChanged(ref _var, value, PropertyChanged);
    }
    private double? _var;

    public Option<NormalErrorModel> ErrorModel
    {
      get => _errorModel;
      set => this.RaiseAndSetIfChanged(ref _errorModel, value, PropertyChanged);
    }
    private Option<NormalErrorModel> _errorModel;

    public IErrorModel? ErrorModelUnsafe
    {
      get => _errorModel.Match(em => em, () => default(IErrorModel));
      set => ErrorModel = value == default ? None : Some(RequireInstanceOf<NormalErrorModel>(value));
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
          Sigma = IsNaN(errorModel.Sigma) ? default(double?) : errorModel.Sigma;
          StepInitializer = IsNaN(errorModel.StepInitializer) ? default(double?) : errorModel.StepInitializer;
          Var = Sigma * Sigma;
        },
        () =>
        {
          Sigma = default;
          StepInitializer = default;
          Var = default;
        });
    }

    private void ObserveErrorModelParameters(object? _)
    {
      if (Sigma.HasValue && StepInitializer.HasValue)
      {
        ErrorModel = new NormalErrorModel(Sigma.Value, StepInitializer.Value);
      }

      Var = Sigma * Sigma;
    }

    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
