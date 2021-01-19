using RVis.Client;
using RVis.Data;
using System.Threading.Tasks;

namespace RVis.Model.Test
{
  internal static class DistributionImpl
  {
    internal static async Task<NumDataColumn[]> GetNumDataAsync(string code)
    {
      using var server = new RVisServer();
      var client = await server.OpenChannelAsync();
      return await client.EvaluateNumDataAsync(code);
    }
  }
}
