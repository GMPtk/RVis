using LanguageExt;
using System;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class SensitivityDesign
  {
    internal SensitivityDesign(DateTime createdOn, byte[] serializedDesign, Arr<DesignParameter> designParameters, int sampleSize, DataTable samples)
    {
      CreatedOn = createdOn;
      SerializedDesign = serializedDesign;
      DesignParameters = designParameters;
      SampleSize = sampleSize;
      Samples = samples;
    }

    internal DateTime CreatedOn { get; }
    internal byte[] SerializedDesign { get; }
    internal Arr<DesignParameter> DesignParameters { get; }
    internal int SampleSize { get; }
    internal DataTable Samples { get; }

    public override string ToString() => this.GetDescription();
  }
}
