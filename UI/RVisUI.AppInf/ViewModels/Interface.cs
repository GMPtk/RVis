using LanguageExt;

namespace RVisUI.AppInf
{
  public interface IParameterDistributionViewModel
  {
    Arr<string> DistributionNames { get; }
    int SelectedDistributionName { get; set; }
    IDistributionViewModel DistributionViewModel { get; set; }
    Option<ParameterState> ParameterState { get; set; }
  }
}
