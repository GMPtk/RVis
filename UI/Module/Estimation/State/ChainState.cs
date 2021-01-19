using LanguageExt;
using System.Collections.Generic;
using System.Data;

namespace Estimation
{
  internal partial class ChainState
  {
    internal ChainState(
      int no, 
      Arr<ModelParameter> modelParameters, 
      Arr<ModelOutput> modelOutputs, 
      DataTable? chainData,
      DataTable? errorData,
      IDictionary<string, DataTable>? posteriorData
      )
    {
      No = no;
      ModelParameters = modelParameters;
      ModelOutputs = modelOutputs;
      ChainData = chainData;
      ErrorData = errorData;
      PosteriorData = posteriorData;
    }

    internal int No { get; }
    internal Arr<ModelParameter> ModelParameters { get; }
    internal Arr<ModelOutput> ModelOutputs { get; }
    internal DataTable? ChainData { get; }
    internal DataTable? ErrorData { get; }
    internal IDictionary<string, DataTable>? PosteriorData { get; }
  }
}
