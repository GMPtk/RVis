using RVis.Model;
using RVisUI.Model;
using System;
using System.Runtime.CompilerServices;

#nullable disable

namespace RVisUI.AppInf.Design
{
  public sealed class AppService : IAppService
  {
    public IObservable<long> SecondInterval => throw new NotImplementedException();

    public IRVisServerPool RVisServerPool => throw new NotImplementedException();

    public bool AskUserYesNoQuestion(string question, string prompt, string about)
    {
      throw new NotImplementedException();
    }

    public bool BrowseForDirectory(string startPath, out string pathToDirectory)
    {
      throw new NotImplementedException();
    }

    public bool CheckAccess()
    {
      throw new NotImplementedException();
    }

    public IReactiveSafeInvoke GetReactiveSafeInvoke()
    {
      return new ReactiveSafeInvoke();
    }

    public void Initialize()
    {
      throw new NotImplementedException();
    }

    public void Notify(NotificationType type, string about, string subject, string detail, object originatingViewModel = null)
    {
      throw new NotImplementedException();
    }

    public void Notify(string about, string subject, Exception ex, object originatingViewModel = null)
    {
      throw new NotImplementedException();
    }

    public bool OpenFile(string purpose, string initialDirectory, string filter, out string pathToFile)
    {
      throw new NotImplementedException();
    }

    public void ResetRVisServerPool()
    {
      throw new NotImplementedException();
    }

    public Action<T> SafeInvoke<T>(Action<T> action, [CallerMemberName] string subject = "")
    {
      throw new NotImplementedException();
    }

    public bool SaveFile(string purpose, string initialDirectory, string filter, string extension, out string pathToFile)
    {
      throw new NotImplementedException();
    }

    public void ScheduleAction(Action action)
    {
      throw new NotImplementedException();
    }

    public void ScheduleLowPriorityAction(Action action)
    {
      throw new NotImplementedException();
    }

    public bool ShowDialog(object view, object viewModel, object parentViewModel)
    {
      throw new NotImplementedException();
    }

    public bool ShowDialog(object viewModel, object parentViewModel)
    {
      throw new NotImplementedException();
    }
  }
}
