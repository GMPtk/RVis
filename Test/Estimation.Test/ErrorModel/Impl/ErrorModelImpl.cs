using RVis.Client;
using RVis.Data;
using System.Threading.Tasks;

namespace Estimation.Test
{
  internal static class ErrorModelImpl
  {
    internal static async Task<NumDataColumn[]> GetNumDataAsync(string code)
    {
      using var server = new RVisServer();
      var client = await server.OpenChannelAsync();
      return await client.EvaluateNumDataAsync(code);
    }
  }
}
