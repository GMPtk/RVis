using LanguageExt;
using System;
using System.Data;

namespace Sampling
{
  internal sealed partial class SamplingDesign
  {
    internal SamplingDesign(
      DateTime createdOn, 
      Arr<DesignParameter> designParameters, 
      int? seed, 
      DataTable samples, 
      Arr<int> noDataIndices
      ) : this(createdOn, seed, samples, noDataIndices)
    {
      DesignParameters = designParameters;
    }

    internal SamplingDesign(
      DateTime createdOn,
      LatinHypercubeDesign latinHypercubeDesign,
      int? seed,
      DataTable samples,
      Arr<int> noDataIndices
      ) : this(createdOn, seed, samples, noDataIndices)
    {
      LatinHypercubeDesign = latinHypercubeDesign;
    }

    private SamplingDesign(
      DateTime createdOn,
      int? seed,
      DataTable samples,
      Arr<int> noDataIndices
      )
    {
      CreatedOn = createdOn;
      Seed = seed;
      Samples = samples;
      NoDataIndices = noDataIndices;
    }

    internal DateTime CreatedOn { get; }
    internal Arr<DesignParameter> DesignParameters { get; }
    internal LatinHypercubeDesign LatinHypercubeDesign { get; }
    internal int? Seed { get; }
    internal DataTable Samples { get; }
    internal Arr<int> NoDataIndices { get; }

    public override string ToString() => this.GetDescription();
  }
}
