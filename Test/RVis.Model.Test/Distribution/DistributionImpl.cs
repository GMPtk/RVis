using RVis.Client;
using RVis.Data;

namespace RVis.Model.Test
{
  internal static class DistributionImpl
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
