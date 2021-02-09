using Grpc.Core;
using Grpc.Net.Client;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.R;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static RVis.Base.Check;
using static RVis.Client.NativeMethods;
using static RVis.R.ROps;

namespace RVis.Client
{
  public sealed partial class RVisServer : IRVisServer, IDisposable
  {
    static RVisServer() =>
      _idSource = (Environment.ProcessId % 100000) * 1000;

    public RVisServer()
    {
      ID = ++_idSource;
      _executableDirectory = Assembly.GetExecutingAssembly().GetDirectory();
    }

    public RVisServer(string executableDirectory)
    {
      ID = ++_idSource;
      _executableDirectory = executableDirectory;
    }

    public int ID { get; private set; }

    public bool IsUp
    {
      get
      {
        return
          _channel != null &&
          _rOpsClient != null &&
          _server != null &&
          !_server.HasExited;
      }
    }

    public async Task<IRVisClient> OpenChannelAsync(CancellationToken cancellationToken = default)
    {
      EnsureServer();

      if (null == _rOpsClient)
      {
        if (_channel is not null)
        {
          //await _channel.ShutdownAsync();
          //_channel.Dispose();
          _channel = null;
        }

        //var socketPath = Path.Combine(Path.GetTempPath(), $"rvis.svr.nix.sock.{ID}.tmp");

        //var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        //{
        //  HttpHandler = CreateHttpHandler(socketPath),
        //  MaxReceiveMessageSize = null
        //});

        var channel = new GrpcDotNetNamedPipes.NamedPipeChannel(".", $"rvis.svr.pipe.{ID}");

        var rOpsClient = new ROpsClient(channel);

        await rOpsClient.PingAsync(
          new PingRequest { Pid = Environment.ProcessId },
          new CallOptions().WithWaitForReady()
          );

        _channel = channel;
        _rOpsClient = rOpsClient;
      }

      return new RVisClientProxy(this);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
      if (_rOpsClient is not null)
      {
        try
        {
          await _rOpsClient.ShutDownAsync(
            new ShutDownRequest(),
            default,
            default,
            cancellationToken
            );
        }
        catch (Exception) { }
        finally
        {
          _rOpsClient = null;
        }
      }

      if (_channel is not null)
      {
        try
        {
          //await _channel.ShutdownAsync();
          //_channel.Dispose();
        }
        catch (Exception) { }
        finally
        {
          _channel = null;
        }
      }

      try
      {
        _server?.Kill();
      }
      catch (Exception) { }
      finally
      {
        _server = null;
      }

      ID = ++_idSource;
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    //private static SocketsHttpHandler CreateHttpHandler(string socketPath)
    //{
    //  var endPoint = new UnixDomainSocketEndPoint(socketPath);

    //  var connectionFactory = new UnixDomainSocketConnectionFactory(endPoint);

    //  return new SocketsHttpHandler
    //  {
    //    ConnectCallback = connectionFactory.ConnectAsync
    //  };
    //}

    private bool DoServiceShutdown()
    {
      var didCallShutdown = false;

      if (_server?.HasExited == true)
      {
        return didCallShutdown;
      }

      if (null != _rOpsClient)
      {
        try
        {
          _rOpsClient.ShutDown(new ShutDownRequest());
          _server?.WaitForExit(300);
          didCallShutdown = true;
        }
        catch (Exception)
        {
        }
        finally
        {
          _rOpsClient = null;
        }
      }

      if (null != _channel)
      {
        try
        {
          //_channel.ShutdownAsync().Wait();
        }
        catch (Exception)
        {
        }
        finally
        {
          _channel = null;
        }
      }

      return didCallShutdown;
    }

    private void DoServerKill(bool killAnyway)
    {
      if (_server != null)
      {
        try
        {
          if (killAnyway)
          {
            _server.Kill();
          }
          else
          {
            var exited = _server.WaitForExit(300);

            if (!exited)
            {
              _server.Kill();
            }
          }
        }
        catch (Exception) { }
        finally
        {
          _server = null;
        }
      }
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        var killAnyway = !DoServiceShutdown();
        DoServerKill(killAnyway);
      }
    }

    private void EnsureServer()
    {
      if (_server?.HasExited == true)
      {
        DoServiceShutdown();
        _server = null;
      }

      if (null == _server)
      {
        var workingDirectory = Path.Combine(_executableDirectory, "R");
        var pathToServer = Path.Combine(workingDirectory, "RVis.Server.exe");

        if (!File.Exists(pathToServer))
        {
          var match = Regex.Match(_executableDirectory, @"(.*\\RVis\\).*");
          RequireTrue(match.Success);
          var pathToRVis = match.Groups[1].Value;

#if DEBUG
          var buildTargetDirectory = "Debug";
#else
          var buildTargetDirectory = "Release";
#endif

          workingDirectory = Path.Combine(pathToRVis, $@"R\RVis.Server\bin\{buildTargetDirectory}\net5.0");

          pathToServer = Path.Combine(workingDirectory, "RVis.Server.exe");

          RequireFile(pathToServer, "Failed to find server executable");
        }

        _server = Process.Start(
          new ProcessStartInfo
          {
            FileName = pathToServer,
            WorkingDirectory = workingDirectory,
            Arguments = ID.ToString()
          });

        RequireNotNull(_server);

        try
        {
          TerminateOnAppExit(_server);
        }
        catch (Exception) { }
      }
    }

    private readonly string _executableDirectory;
    private Process? _server;
    private GrpcDotNetNamedPipes.NamedPipeChannel? _channel; // GrpcChannel? _channel;
    private ROpsClient? _rOpsClient;
    private static int _idSource;
  }
}
