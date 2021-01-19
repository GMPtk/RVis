using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RVisUI.Model
{
  public abstract class ViewModelBase : ReactiveObject, ITaskRunner, IDisposable
  {
    public bool IsRunningTask
    {
      get => _isRunningTask;
      set => this.RaiseAndSetIfChanged(ref _isRunningTask, value);
    }
    private bool _isRunningTask;

    public string? TaskName
    {
      get => _taskName;
      set => this.RaiseAndSetIfChanged(ref _taskName, value);
    }
    private string? _taskName;

    public bool CanCancelTask
    {
      get => _canCancelTask;
      set => this.RaiseAndSetIfChanged(ref _canCancelTask, value);
    }

    private bool _canCancelTask;

    public virtual void HandleCancelTask() { }

    public IObservable<string> TaskMessages => _taskMessages.AsObservable();
    private readonly ISubject<string> _taskMessages = new Subject<string>();

    protected void RaiseTaskMessageEvent(string message) => _taskMessages.OnNext(message);

    public void Dispose()
    {
      GC.SuppressFinalize(this);
      Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          if (_taskMessages is IDisposable taskMessages) taskMessages.Dispose();
        }

        _disposed = true;
      }
    }

    private bool _disposed = false;
  }
}
