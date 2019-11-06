using NLog;
using RVis.Base;
using RVis.Model;
using RVis.ROps;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Windows.Forms;
using static RVis.Model.SvcRes;

namespace RVis.Server
{
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
  public partial class RVisService : IRVisService
  {
    public NameValueArraySvcRes GetRversion()
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return ROpsApi.GetRversion();
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public NameValueArraySvcRes GetInstalledPackages()
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return ROpsApi.GetInstalledPackages();
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public UnitSvcRes Clear()
    {
      try
      {
        ROpsApi.Clear();
      }
      catch (Exception ex)
      {
        _log.Error(ex);
        return ex;
      }

      return Unit;
    }

    public UnitSvcRes GarbageCollect()
    {
      try
      {
        GC.Collect();
        ROpsApi.GarbageCollect();
      }
      catch (Exception ex)
      {
        _log.Error(ex);
        return ex;
      }

      return Unit;
    }

    public UnitSvcRes Shutdown()
    {
      if (null != _rvisServiceCallback)
      {
        try
        {
          ((IClientChannel)_rvisServiceCallback).Abort();
        }
        catch (Exception ex)
        {
          _log.Error(ex);
        }
        finally
        {
          _rvisServiceCallback = null;
        }
      }

      Application.Exit();

      return Unit;
    }

    private static readonly object _rvisServiceLock = new object();
    private readonly ILogger _log = Logging.Create(nameof(RVisService) + ".All");
  }
}
