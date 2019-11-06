namespace Estimation
{
  internal readonly struct ModelOutput
  {
    internal ModelOutput(string name, IErrorModel errorModel)
    {
      Name = name;
      ErrorModel = errorModel;
    }

    internal string Name { get; }
    internal IErrorModel ErrorModel { get; }

    internal ModelOutput GetPerturbed(bool isBurnIn) => 
      new ModelOutput(Name, ErrorModel.GetPerturbed(isBurnIn));

    internal ModelOutput ApplyBias(double bias) =>
      new ModelOutput(Name, ErrorModel.ApplyBias(bias));
  }
}
