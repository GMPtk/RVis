using Grpc.Core;
using Microsoft.Extensions.Logging;
using RVis.R;
using RVis.ROps;
using System;
using System.Threading;
using System.Threading.Tasks;
using static RVis.R.ROps;

namespace RVis.Server
{
  public partial class ROpsService : ROpsBase
  {
    public ROpsService(ILogger<ROpsService> logger)
    {
      _logger = logger;
    }

    public override async Task<RversionReply> GetRversion(RversionRequest _, ServerCallContext __)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new RversionReply();
      try
      {
        var rVersion = ROpsApi.GetRversion();

        reply.Payload = new RversionPayload();
        reply.Payload.Rversion.Add(rVersion);
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(GetRversion));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<InstalledPackagesReply> GetInstalledPackages(InstalledPackagesRequest _, ServerCallContext __)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new InstalledPackagesReply();
      try
      {
        var installedPackages = ROpsApi.GetInstalledPackages();

        reply.Payload = new InstalledPackagesPayload();
        reply.Payload.InstalledPackages.Add(installedPackages);
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(GetInstalledPackages));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<ClearGlobalEnvironmentReply> ClearGlobalEnvironment(ClearGlobalEnvironmentRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new ClearGlobalEnvironmentReply();
      try
      {
        ROpsApi.ClearGlobalEnvironment();
        reply.Payload = new ClearGlobalEnvironmentPayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(ClearGlobalEnvironment));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<GarbageCollectReply> GarbageCollect(GarbageCollectRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new GarbageCollectReply();
      try
      {
        GC.Collect();
        ROpsApi.GarbageCollect();
        reply.Payload = new GarbageCollectPayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(GarbageCollect));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override Task<ShutDownReply> ShutDown(ShutDownRequest request, ServerCallContext context)
    {
      Task.Run(async () =>
      {
        await Task.Delay(50);
        Environment.Exit(0);
      });
      return Task.FromResult(new ShutDownReply { Payload = new ShutDownPayload() });
    }

    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
    {
      return Task.FromResult(new PingReply { Payload = new PingPayload { Pid = Environment.ProcessId } });
    }

    private static Error PopulateError(Exception? ex)
    {
      var error = new Error();

      while (ex != null)
      {
        error.Messages.Add(ex.Message);
        error.StackTraces.Add(ex.StackTrace ?? "(no stack trace)");
        ex = ex.InnerException;
      }

      return error;
    }

    private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
    private readonly ILogger<ROpsService> _logger;
  }
}
