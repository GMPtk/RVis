using System.Data;
using System.Threading.Tasks;

namespace Sampling
{
  internal interface ISampleGenerator
  {
    Task<DataTable> GetSamplesAsync();
  }
}
