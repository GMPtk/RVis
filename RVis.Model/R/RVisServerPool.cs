using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
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
      var serverSlots = servers.Map(s => new ServerSlot(++_id, s) { IsFree = true });

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
            var serverLicense = new ServerLicense(
              serverSlot.ID,
              serverSlot.Server,
              serverSlot.MCSimExecutors,
              (sl) => ExpireLicense(serverSlot, sl)
              );

            serverSlot.IsFree = false;

            _serverLicenses.OnNext((serverLicense, HasExpired: false));

            Log.Debug($"Issued license for server {serverSlot.ID}");

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
          .Find(s => s.ID == serverLicense.ID)
          .AssertSome($"Trying to renew license for defunct server {serverLicense.ID}");

        if (!serverSlot.IsFree) return None;

        var renewedServerLicense = new ServerLicense(
          serverSlot.ID,
          serverSlot.Server,
          serverSlot.MCSimExecutors,
          (sl) => ExpireLicense(serverSlot, sl)
          );

        serverSlot.IsFree = false;

        _serverLicenses.OnNext((renewedServerLicense, HasExpired: false));

        Log.Debug($"Renewed license for server {serverSlot.ID}");

        return renewedServerLicense;
      }
    }

    public IObservable<(ServerLicense ServerLicense, bool HasExpired)> ServerLicenses =>
      _serverLicenses.AsObservable();
    private readonly Subject<(ServerLicense ServerLicense, bool HasExpired)> _serverLicenses =
      new Subject<(ServerLicense ServerLicense, bool HasExpired)>();

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
        _serverLicenses.OnNext((serverLicense, HasExpired: true));
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
              (ServerLicense.NullLicense, HasExpired: true)
              );
        });

        serverSlots.Iter(ss =>
        {
          var task = ss.Server.StopAsync();
          if (disposing)
          {
            task.Wait(500);
          }
          ((IDisposable)ss.Server).Dispose();
          ss.MCSimExecutors.Values.Iter(me => me.Dispose());
          ss.MCSimExecutors.Clear();
        });

        Log.Debug($"{nameof(RVisServerPool)} destroyed pool");
      }
    }

    private Arr<ServerSlot> _serverSlots;
    private readonly object _syncLock = new object();
    private readonly ManualResetEventSlim _mreServerSlots = new ManualResetEventSlim(true);
    private int _id;
    private bool _disposed = false;
  }
}
