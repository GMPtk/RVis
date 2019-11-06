using RVis.Data;
using RVis.Model;

namespace RVis.Server
{
  public partial class RVisService
  {
    public BoolSvcRes IsBusy()
    {
      lock (_rvisServiceLock)
      {
        return null != _rvisServiceCallback;
      }
    }
    
    private IRVisServiceCallback _rvisServiceCallback;
  }
}
