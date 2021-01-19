using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RVis.Base
{
  public static class ProcessHelper
  {
    public static void OpenUrl(string url)
    {
      try
      {
        Process.Start(url);
      }
      catch
      {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          url = url.Replace("&", "^&");
          Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
          Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
          Process.Start("open", url);
        }
        else
        {
          throw;
        }
      }
    }
  }
}
