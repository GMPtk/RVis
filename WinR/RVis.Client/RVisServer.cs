using ProtoBuf.ServiceModel;
using RVis.Base.Extensions;
using RVis.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using static RVis.Base.Check;

namespace RVis.Client
{
  public sealed partial class RVisServer : IRVisServer, IDisposable
  {
    static RVisServer() =>
      _idSource = (Process.GetCurrentProcess().Id % 100000) * 1000;

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

    public IRVisClient OpenChannel()
    {
      lock (_serviceLock)
      {
        RequireNull(_service, "Channel currently open");
        var service = ServiceFactory.CreateChannel();
        var clientChannel = (IClientChannel)service;
        try
        {
          clientChannel.Open();
        }
        catch (Exception) { }
        if (clientChannel.State == CommunicationState.Faulted)
        {
          DoFactoryClose();
          DoServerKill(true);
          service = ServiceFactory.CreateChannel();
          clientChannel = (IClientChannel)service;
          clientChannel.Open();
          if (clientChannel.State == CommunicationState.Faulted)
          {
            throw new InvalidOperationException("Failed to restart server");
          }
        }

        _service = service;
        return new RVisClientProxy(this);
      }
    }

    public bool IsUp =>
      null != _serviceFactory && _serviceFactory.State == CommunicationState.Opened;

    public void Stop()
    {
      _service = null;

      try
      {
        _server?.Kill();
      }
      catch (Exception) { }
      finally
      {
        _server = null;
      }

      try
      {
        var state = _serviceFactory?.State;
        if (state == CommunicationState.Opened)
        {
          _serviceFactory.Abort();
        }
      }
      catch (Exception) { }
      finally
      {
        _serviceFactory = null;
      }

      ID = ++_idSource;
    }

    private DuplexChannelFactory<IRVisService> ServiceFactory
    {
      get
      {
        if (null == _serviceFactory)
        {
          lock (_serviceLock)
          {
            EnsureServer();
            _serviceFactory = MakeChannelFactory(this, ID.ToString("00000000"));
          }
        }
        return _serviceFactory;
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private bool DoServiceShutdown()
    {
      var didCallShutdown = false;

      if (null != _service)
      {
        try
        {
          var clientChannel = (IClientChannel)_service;
          var state = clientChannel.State;
          if (state == CommunicationState.Opened || state == CommunicationState.Created)
          {
            _service.Shutdown();
            didCallShutdown = true;
            _server.WaitForExit(300);
            // clientChannel.Abort();
          }
          state = clientChannel.State;
          if (state != CommunicationState.Faulted)
          {
            clientChannel.Dispose();
          }
        }
        catch (Exception)
        {
        }
        finally { _service = null; }
      }

      return didCallShutdown;
    }

    private void DoFactoryClose()
    {
      if (null != _serviceFactory)
      {
        try
        {
          var state = _serviceFactory.State;
          if (state == CommunicationState.Opened)
          {
            _serviceFactory.Abort();
          }
        }
        catch (Exception) { }
        finally { _serviceFactory = null; }
      }
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
        if (null != _server && !_server.HasExited && null != _serviceFactory && null == _service)
        {
          try
          {
            _service = _serviceFactory.CreateChannel();
            ((IClientChannel)_service).Open();
          }
          catch (Exception)
          {
            _service = null;
          }
        }

        var killAnyway = !DoServiceShutdown();
        DoFactoryClose();
        DoServerKill(killAnyway);
      }
    }

    private void EnsureServer()
    {
      if (null == _server || _server.HasExited)
      {
        try
        {
          var wait = false;

          using (var factory = MakeChannelFactory(this, ID.ToString("00000000")))
          {
            var channel = factory.CreateChannel();
            var clientChannel = (IClientChannel)channel;
            if (clientChannel.State == CommunicationState.Opened)
            {
              channel.Shutdown();
              clientChannel.Close();
              wait = true;
            }
          }

          if (wait) System.Threading.Thread.Sleep(180);
        }
        catch (Exception) { }

        var pathToServer = Path.Combine(_executableDirectory, "RVis.Server.exe");
        if (!File.Exists(pathToServer))
        {
          var bitness = Environment.Is64BitProcess ? @"x64\" : string.Empty;
          var bitnessUp = Environment.Is64BitProcess ? @"..\" : string.Empty;

#if DEBUG
          var buildTargetDirectory = "Debug";
#else
          var buildTargetDirectory = "Release";
#endif

          var serverDirectory = bitnessUp + @"..\..\..\..\WinR\RVis.Server\bin\" + bitness + buildTargetDirectory;

          pathToServer = Path.Combine(
            _executableDirectory,
            serverDirectory,
            "RVis.Server.exe"
            );

          RequireFile(pathToServer, "Failed to find server executable");
        }

        _server = Process.Start(
          new ProcessStartInfo
          {
            FileName = pathToServer,
            Arguments = ID.ToString("00000000")
          });

        try
        {
          NativeMethods.TerminateOnAppExit(_server);
        }
        catch (Exception) { }
      }
      _server.WaitForInputIdle();
    }

    private static DuplexChannelFactory<IRVisService> MakeChannelFactory(object implementation, string id)
    {
      var serviceFactory = new DuplexChannelFactory<IRVisService>(
        new InstanceContext(implementation),
        ServiceHelper.MakeNetNamedPipeBinding(),
        new EndpointAddress(ServiceHelper.GetRVisServiceNamedPipeAddress(id))
        );
      serviceFactory.Endpoint.EndpointBehaviors.Add(new ProtoEndpointBehavior());
      return serviceFactory;
    }

    private string _executableDirectory;
    private Process _server;
    private IRVisService _service;
    private object _serviceLock = new object();
    private DuplexChannelFactory<IRVisService> _serviceFactory;
    private static int _idSource;
  }
}
