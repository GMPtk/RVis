using LanguageExt;
using System;
using static LanguageExt.Prelude;

namespace RVisUI.AppInf.Design
{
  public sealed class ParameterDistributionViewModel : IParameterDistributionViewModel
  {
    public Arr<string> DistributionNames => Array("Normal", "Log normal");
    public int SelectedDistributionName { get => 1; set => throw new NotImplementedException(); }
    public IDistributionViewModel? DistributionViewModel
    {
      get => new LogNormalDistributionViewModel()
      {
        Variable = "x",
        Mu = 2.0,
        Sigma = 0.6
      };
      set => throw new NotImplementedException();
    }
    public Option<ParameterState> ParameterState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
