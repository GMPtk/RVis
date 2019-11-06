using RVis.Client;
using RVis.Data;

namespace Estimation.Test
{
  internal static class ErrorModelImpl
  {
    internal static NumDataColumn[] GetNumData(string code)
    {
      using (var server = new RVisServer())
      {
        using (var client = server.OpenChannel())
        {
          return client.EvaluateNumData(code);
        }
      }
    }
  }
}
