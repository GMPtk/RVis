using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Model.Logger;

namespace RVis.Model
{
  public sealed class RVisServerPool : IRVisServerPool, IDisposable
  {
    public RVisServerPool()
    {
    }

    public void CreatePool(Func<IRVisServer> serverFactory, int size)
    {
      RequireTrue(size >= 0);
      RequireTrue(_serverSlots.IsEmpty);

      var servers = Range(0, size).Map(_ => serverFactory()).ToArr();
      var serverSlots = servers.Map(s => new ServerSlot(s) { IsFree = true });

      lock (_syncLock)
      {
        _serverSlots = serverSlots;
      }

      Log.Debug($"{nameof(RVisServerPool)} initialized {size} instances");
    }

    public void DestroyPool() => DestroyPool(disposing: false);

    public WaitHandle SlotFree => _mreServerSlots.WaitHandle;

    public Option<ServerLicense> RequestServer()
    {
      lock (_syncLock)
      {
        var freeServerSlot = _serverSlots.Find(s => s.IsFree);

        return freeServerSlot.Match(
          serverSlot =>
          {
            var client = serverSlot.Server.OpenChannel();
            var serverLicense = new ServerLicense(client, (sl) => ExpireLicense(serverSlot, sl));
            serverSlot.IsFree = false;
            _serverLicenses.OnNext((serverSlot.Server.ID, serverLicense, HasExpired: false));

            Log.Debug($"Issued license for server {serverSlot.Server.ID}");

            return Some(serverLicense);
          },
          () =>
          {
            _mreServerSlots.Reset();
            return None;
          });
      }
    }

    public Option<ServerLicense> RenewServerLicense(ServerLicense serverLicense)
    {
      lock (_syncLock)
      {
        var serverSlot = _serverSlots
          .Find(s => s.Server.ID == serverLicense.Client.ID)
          .AssertSome($"Trying to renew license for defunct server {serverLicense.Client.ID}");

        if (!serverSlot.IsFree) return None;

        var client = serverSlot.Server.OpenChannel();
        var renewedServerLicense = new ServerLicense(client, (sl) => ExpireLicense(serverSlot, sl));
        serverSlot.IsFree = false;
        _serverLicenses.OnNext((serverSlot.Server.ID, renewedServerLicense, HasExpired: false));

        Log.Debug($"Renewed license for server {serverSlot.Server.ID}");

        return renewedServerLicense;
      }
    }

    public IObservable<(int ServerID, ServerLicense ServerLicense, bool HasExpired)> ServerLicenses =>
      _serverLicenses.AsObservable();
    private readonly Subject<(int ServerID, ServerLicense ServerLicense, bool HasExpired)> _serverLicenses =
      new Subject<(int ServerID, ServerLicense ServerLicense, bool HasExpired)>();

    public void Dispose() => Dispose(disposing: true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          DestroyPool(disposing);
          _mreServerSlots.Dispose();
          _serverLicenses.Dispose();
        }

        _disposed = true;
      }
    }

    private void ExpireLicense(ServerSlot serverSlot, ServerLicense serverLicense)
    {
      lock (_syncLock)
      {
        _serverLicenses.OnNext((serverSlot.Server.ID, serverLicense, HasExpired: true));
        serverLicense.Client.Dispose();
        serverSlot.IsFree = true;

        if (_serverSlots.IndexOf(serverSlot).IsFound()) _mreServerSlots.Set();

        Log.Debug($"Expired license for server {serverSlot.Server.ID}");
      }
    }

    private void DestroyPool(bool disposing)
    {
      lock (_syncLock)
      {
        if (0 == _serverSlots.Count) return;

        var serverSlots = _serverSlots;
        _serverSlots = default;

        serverSlots.Iter(ss =>
        {
          if (!ss.IsFree)
            _serverLicenses.OnNext(
              (ss.Server.ID, ServerLicense.NullLicense, HasExpired: true)
              );
        });

        void StopDisposeServers() =>
          serverSlots.Map(ss => ss.Server).Iter(s =>
          {
            s.Stop();
            (s as IDisposable).Dispose();
          });

        if (disposing)
        {
          // shutting down so block
          StopDisposeServers();
        }
        else
        {
          // user api call so don't block
          Task.Run(StopDisposeServers);
        }

        Log.Debug($"{nameof(RVisServerPool)} destroyed pool");
      }
    }

    private Arr<ServerSlot> _serverSlots;
    private readonly object _syncLock = new object();
    private readonly ManualResetEventSlim _mreServerSlots = new ManualResetEventSlim(true);
    private bool _disposed = false;
  }
}
