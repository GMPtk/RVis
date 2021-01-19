using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RVis.Base;
using System.IO;

namespace RVis.Server
{
  public class Startup
  {
    public void ConfigureServices(IServiceCollection services) =>
      services.AddGrpc(options =>
      {
        options.MaxReceiveMessageSize = null;
        options.EnableDetailedErrors = true;
      });

    public void Configure(IApplicationBuilder app, IWebHostEnvironment _)
    {
      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapGrpcService<ROpsService>();
      });

      var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
      loggerFactory!.AddFile(GetLoggerFile());
    }

    internal static string GetLoggerFile()
    {
      var directory = Path.Combine(DirectoryOps.ApplicationDataDirectory.FullName, "Log");
      if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
      return Path.Combine(directory, "rvis.svr-{Date}.txt");
    }
  }
}
