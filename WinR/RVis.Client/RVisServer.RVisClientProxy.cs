using RVis.Model;
using System;
using System.ServiceModel;
using static RVis.Base.Check;

namespace RVis.Client
{
  public sealed partial class RVisServer
  {
    private sealed partial class RVisClientProxy : IRVisClient
    {
      internal RVisClientProxy(RVisServer real) => _real = real;

      public int ID => _real.ID;

      public (string Name, string Value)[] GetRversion()
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.GetRversion();

          return svcRes.Return();
        }
      }

      public (string Package, string Version)[] GetInstalledPackages()
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.GetInstalledPackages();

          return svcRes.Return();
        }
      }

      public void Clear()
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          _real._service.Clear();
        }
      }

      public void GarbageCollect()
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          _real._service.GarbageCollect();
        }
      }

      public void StopServer()
      {
        _real.Stop();
      }

      public void Dispose() => Dispose(true);

      private void Dispose(bool disposing)
      {
        if (disposing)
        {
          lock (_real._serviceLock)
          {
            if (null != _real._service)
            {
              try
              {
                var clientChannel = (IClientChannel)_real._service;
                if (clientChannel.State == CommunicationState.Opened)
                {
                  clientChannel.Close();
                }
                if (clientChannel.State != CommunicationState.Faulted)
                {
                  clientChannel.Dispose();
                }
              }
              catch (Exception) { }
              finally { _real._service = null; }
            }
          }
        }
      }

      private readonly RVisServer _real;
    }
  }
}
