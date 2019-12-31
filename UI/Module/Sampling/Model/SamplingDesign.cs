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
      LatinHypercubeDesign latinHypercubeDesign,
      RankCorrelationDesign rankCorrelationDesign,
      int? seed, 
      DataTable samples, 
      Arr<int> noDataIndices
      )
    {
      CreatedOn = createdOn;
      DesignParameters = designParameters;
      LatinHypercubeDesign = latinHypercubeDesign;
      RankCorrelationDesign = rankCorrelationDesign;
      Seed = seed;
      Samples = samples;
      NoDataIndices = noDataIndices;
    }

    internal DateTime CreatedOn { get; }
    internal Arr<DesignParameter> DesignParameters { get; }
    internal LatinHypercubeDesign LatinHypercubeDesign { get; }
    internal RankCorrelationDesign RankCorrelationDesign { get; }
    internal int? Seed { get; }
    internal DataTable Samples { get; }
    internal Arr<int> NoDataIndices { get; }

    public override string ToString() => this.GetDescription();

    public SamplingDesign With(Arr<int> noDataIndices)
    {
      return new SamplingDesign(
        CreatedOn,
        DesignParameters,
        LatinHypercubeDesign,
        RankCorrelationDesign,
        Seed,
        Samples,
        noDataIndices
        );
    }
  }
}
