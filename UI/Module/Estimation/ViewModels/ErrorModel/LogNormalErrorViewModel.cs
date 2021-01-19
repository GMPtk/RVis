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
  [DisplayName("Log Normal")]
  internal sealed class LogNormalErrorViewModel : ILogNormalErrorViewModel, INotifyPropertyChanged
  {
    internal LogNormalErrorViewModel(IAppService appService)
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
        .WhenAny(vm => vm.SigmaLog, vm => vm.StepInitializer, (_, __) => default(object))
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object?>(
            ObserveErrorModelParameters
            )
          )

        );
    }

    public LogNormalErrorViewModel()
      : this(new RVisUI.AppInf.Design.AppService())
    {
      RequireTrue(IsInDesignMode);
      ErrorModel = LogNormalErrorModel.Default;
    }

    public ErrorModelType ErrorModelType => ErrorModelType.LogNormal;

    public double? SigmaLog
    {
      get => _sigmaLog;
      set => this.RaiseAndSetIfChanged(ref _sigmaLog, value, PropertyChanged);
    }
    private double? _sigmaLog;

    public double? StepInitializer
    {
      get => _stepInitializer;
      set => this.RaiseAndSetIfChanged(ref _stepInitializer, value, PropertyChanged);
    }
    private double? _stepInitializer;

    public double? VarLog
    {
      get => _varLog;
      private set => this.RaiseAndSetIfChanged(ref _varLog, value, PropertyChanged);
    }
    private double? _varLog;

    public Option<LogNormalErrorModel> ErrorModel
    {
      get => _errorModel;
      set => this.RaiseAndSetIfChanged(ref _errorModel, value, PropertyChanged);
    }
    private Option<LogNormalErrorModel> _errorModel;

    public IErrorModel? ErrorModelUnsafe
    {
      get => _errorModel.Match(em => em, () => default(IErrorModel));
      set => ErrorModel = value == default ? None : Some(RequireInstanceOf<LogNormalErrorModel>(value));
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
          SigmaLog = IsNaN(errorModel.SigmaLog) ? default(double?) : errorModel.SigmaLog;
          StepInitializer = IsNaN(errorModel.StepInitializer) ? default(double?) : errorModel.StepInitializer;
          VarLog = SigmaLog * SigmaLog;
        },
        () =>
        {
          SigmaLog = default;
          StepInitializer = default;
          VarLog = default;
        });
    }

    private void ObserveErrorModelParameters(object? _)
    {
      if (SigmaLog.HasValue && StepInitializer.HasValue)
      {
        ErrorModel = new LogNormalErrorModel(SigmaLog.Value, StepInitializer.Value);
      }

      VarLog = SigmaLog * SigmaLog;
    }

    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
