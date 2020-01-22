using System;
using System.Collections.Generic;

namespace RVis.Model
{
  public sealed class ServerLicense : IDisposable
  {
    public readonly static ServerLicense NullLicense = new ServerLicense();

    internal ServerLicense(
      int id,
      IRVisServer rServer,
      IDictionary<Simulation, MCSimExecutor> mcsimExecutors,
      Action<ServerLicense> expireLicense
      )
    {
      ID = id;
      _rServer = rServer;
      _mcsimExecutors = mcsimExecutors;
      _expireLicense = expireLicense;
    }

    public int ID { get; }

    public bool IsCurrent => !_disposed;

    public IRVisClient GetRClient()
    {
      if (_rClient == default)
      {
        _rClient = _rServer.OpenChannel();
      }

      return _rClient;
    }

    public MCSimExecutor GetMCSimExecutor(Simulation simulation)
    {
      if (!_mcsimExecutors.ContainsKey(simulation))
      {
        var mcsimExecutor = new MCSimExecutor(
          simulation.PathToSimulation, 
          simulation.SimConfig
          );

        _mcsimExecutors.Add(simulation, mcsimExecutor);
      }

      return _mcsimExecutors[simulation];
    }

    public void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _rClient?.Dispose();
          _expireLicense(this);
        }

        _disposed = true;
      }
    }

    public void Dispose() => Dispose(true);

    private ServerLicense() { }

    private bool _disposed = false;
    private readonly Action<ServerLicense> _expireLicense;
    private readonly IRVisServer _rServer;
    private IRVisClient _rClient;
    private readonly IDictionary<Simulation, MCSimExecutor> _mcsimExecutors;
  }
}
