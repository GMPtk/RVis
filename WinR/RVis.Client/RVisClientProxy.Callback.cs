using RVis.Data;
using RVis.Model;
using System;

namespace RVis.Client
{
  public sealed partial class RVisServer : IRVisServiceCallback
  {
    public void NotifyGeneratedOutput(NumDataTable[] generatedOutput)
    {
      //_onNotifyGeneratedOutput(_id, generatedOutput);
    }

    public void NotifyFault(string message, string innerMessage)
    {
      //_onFailure(_id, message, innerMessage);
    }

    //private Action<int, NumDataTable[]> _onNotifyGeneratedOutput;
    //private Action<int, string, string> _onFailure;
  }
}
