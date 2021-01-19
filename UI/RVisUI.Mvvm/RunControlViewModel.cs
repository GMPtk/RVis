using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using static RVis.Base.Check;

namespace RVisUI.Mvvm
{
  public sealed class RunControlViewModel : ReactiveObject, IRunControlViewModel, IDisposable
  {
    public RunControlViewModel(IAppState appState, IAppService appService)
    {
      _appState = appState;

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _appState
          .ObservableForProperty(@as => @as.RunControl)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveAppStateRunControl
              )
            )

        );
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }
    private bool _isVisible;

    public ObservableCollection<Tuple<DateTime, string>> Messages { get; } = new();

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

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

    private void ObserveAppStateRunControl(object obj)
    {
      if (_appState.RunControl is not null)
      {
        RequireNotNull(SynchronizationContext.Current);

        Messages.Clear();

        var subscription = _appState.RunControl.Messages
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(ObserveRunControlMessage);

        _subscriptions.Add(subscription);

        IsVisible = true;
      }
    }

    private void ObserveRunControlMessage((DateTime Timestamp, string Message) t)
    {
      var entry = Tuple.Create(t.Timestamp, t.Message);

      if (t.Timestamp > _lastTimeStamp)
      {
        Messages.Add(entry);
        _lastTimeStamp = t.Timestamp;
      }
      else
      {
        var index = Messages.FindIndex(u => u.Item1 == t.Timestamp);
        Messages[index] = entry;
      }
    }

    private readonly IAppState _appState;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly CompositeDisposable _subscriptions;
    private DateTime _lastTimeStamp = DateTime.MinValue;
    private bool _disposed;
  }
}
