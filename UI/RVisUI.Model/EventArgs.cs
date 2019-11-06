using System;

namespace RVisUI.Model
{
  public class RunningTaskMessageEventArgs : EventArgs
  {
    public RunningTaskMessageEventArgs(string message) => 
      Message = message;

    public string Message { get; }
  }
}
