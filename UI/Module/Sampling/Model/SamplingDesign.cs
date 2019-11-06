using LanguageExt;
using System;
using System.Data;

namespace Sampling
{
  internal sealed partial class SamplingDesign
  {
    internal SamplingDesign(DateTime createdOn, Arr<DesignParameter> designParameters, int? seed, DataTable samples, Arr<int> noDataIndices)
    {
      CreatedOn = createdOn;
      DesignParameters = designParameters;
      Seed = seed;
      Samples = samples;
      NoDataIndices = noDataIndices;
    }

    internal DateTime CreatedOn { get; }
    internal Arr<DesignParameter> DesignParameters { get; }
    internal int? Seed { get; }
    internal DataTable Samples { get; }
    internal Arr<int> NoDataIndices { get; }

    public override string ToString() => this.GetDescription();
  }
}
