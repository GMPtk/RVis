using LanguageExt;
using System;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class SensitivityDesign
  {
    internal SensitivityDesign(
      DateTime createdOn, 
      Arr<byte[]> serializedDesigns, 
      Arr<DesignParameter> designParameters, 
      SensitivityMethod sensitivityMethod,
      string methodParameters,
      Arr<DataTable> samples
      )
    {
      CreatedOn = createdOn;
      SerializedDesigns = serializedDesigns;
      DesignParameters = designParameters;
      SensitivityMethod = sensitivityMethod;
      MethodParameters = methodParameters;
      Samples = samples;
    }

    internal DateTime CreatedOn { get; }
    internal Arr<byte[]> SerializedDesigns { get; }
    internal Arr<DesignParameter> DesignParameters { get; }
    internal SensitivityMethod SensitivityMethod { get; }
    internal string MethodParameters { get; }
    internal Arr<DataTable> Samples { get; }

    public override string ToString() => this.GetDescription();
  }
}
