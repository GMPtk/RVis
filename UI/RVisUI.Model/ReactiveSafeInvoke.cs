using System;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using static RVis.Base.Check;
using static RVisUI.Model.Logger;

namespace RVisUI.Model
{
  public sealed class ReactiveSafeInvoke : IReactiveSafeInvoke
  {
    public bool React { get; private set; } = true;

    public IDisposable SuspendedReactivity
    {
      get
      {
        RequireTrue(React);
        React = false;
        return Disposable.Create(() => React = true);
      }
    }

    public Action<T> SuspendAndInvoke<T>(Action<T> action, [CallerMemberName] string subject = "") =>
    t =>
    {
      if (!React) return;

      React = false;

      try
      {
        action(t);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Observer fault. Subject: {subject}.");
      }
      finally
      {
        React = true;
      }
    };
  }
}
