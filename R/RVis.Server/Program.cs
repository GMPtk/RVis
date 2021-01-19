using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using static RVis.Base.Check;

namespace RVis.Server
{
  public class Program
  {
    public static void Main(string[] args)
    {
      RequireTrue(args.Length >= 1);
      RequireTrue(int.TryParse(args[0], out int id));

      var loggerFactory = new LoggerFactory();
      loggerFactory!.AddFile(Startup.GetLoggerFile());

      var logger = loggerFactory.CreateLogger<ROpsService>();

      var server = new GrpcDotNetNamedPipes.NamedPipeServer($"rvis.svr.pipe.{id}");
      R.ROps.BindService(server.ServiceBinder, new ROpsService(logger));
      server.Start();

      //CreateHostBuilder(args, id).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args, int id) =>
      Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
          webBuilder.UseStartup<Startup>();

          webBuilder.ConfigureKestrel(options =>
          {
            var socketPath = Path.Combine(Path.GetTempPath(), $"rvis.svr.nix.sock.{id}.tmp");

            if (File.Exists(socketPath))
            {
              File.Delete(socketPath);
            }

            options.ListenUnixSocket(socketPath);
          });
        });
  }
}
