using NLog;
using ProtoBuf.ServiceModel;
using RVis.Base;
using RVis.Model;
using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RVis.Server
{
  class Program
  {
    private static string _serverID;
    private static ILogger _log;

    [STAThread]
    static void Main(string[] args)
    {
      if (RecordRuntimeExceptions) ConfigureDiagnostics();

      Logging.Configure(
        nameof(Program),
        nameof(RVisService),
        nameof(ROps.ROpsApi)
        );

      _log = Logging.Create(nameof(Program) + ".All");

      if (0 == args.Length)
      {
        _serverID = "RVisSrvPub";
        RunAsPublication();
      }
      else
      {
        _serverID = args[0];
        RunAsServer();
      }
    }

    private static void RunAsServer()
    {
      using (var host = new ServiceHost(typeof(RVisService)))
      {
        var endpoint = host.AddServiceEndpoint(
          typeof(IRVisService),
          ServiceHelper.MakeNetNamedPipeBinding(),
          ServiceHelper.GetRVisServiceNamedPipeAddress(_serverID)
          );
        endpoint.EndpointBehaviors.Add(new ProtoEndpointBehavior());

        host.Open();

        Application.Run();

        host.Close();
      }
    }

    private static bool RecordRuntimeExceptions
    {
      get
      {
        var recordRuntimeExceptions = ConfigurationManager.AppSettings["RVis.Server.Program.RecordRuntimeExceptions"];
        return String.IsNullOrEmpty(recordRuntimeExceptions) ?
          false :
          bool.Parse(recordRuntimeExceptions);
      }
    }

    private static void ConfigureDiagnostics()
    {
      Application.ThreadException += (s, e) => HandleFault(e.Exception);
      AppDomain.CurrentDomain.UnhandledException += (s, ee) =>
        HandleFault(
          ee.ExceptionObject is Exception ?
          ee.ExceptionObject as Exception :
          new Exception((ee.ExceptionObject ?? "Unknown error").ToString())
          );
      TaskScheduler.UnobservedTaskException += (s, eee) => HandleFault(eee.Exception);
    }

    private static void HandleFault(Exception ex) => 
      _log.Error(ex);

    private static void RunAsPublication()
    {
      using (var host = new ServiceHost(typeof(RVisService), new Uri("net.pipe://localhost/rvisservice")))
      {
        var smb = new ServiceMetadataBehavior
        {
          HttpGetEnabled = false
        };
        smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
        host.Description.Behaviors.Add(smb);

        var endpoint = host.AddServiceEndpoint(
          ServiceMetadataBehavior.MexContractName,
          MetadataExchangeBindings.CreateMexNamedPipeBinding(),
          "mex"
        );

        endpoint = host.AddServiceEndpoint(
          typeof(IRVisService),
          ServiceHelper.MakeNetNamedPipeBinding(),
          "disc"
          );

        host.Open();
        Application.Run();
        host.Close();
      }
    }
  }
}
