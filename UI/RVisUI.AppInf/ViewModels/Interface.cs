using LanguageExt;
using System.Windows.Input;

namespace RVisUI.AppInf
{
  public interface ISelectableItemViewModel
  {
    string Label { get; }
    string SortKey { get; }
    ICommand Select { get; }
    bool IsSelected { get; set; }
  }

  public interface IParameterDistributionViewModel
  {
    Arr<string> DistributionNames { get; }
    int SelectedDistributionName { get; set; }
    IDistributionViewModel? DistributionViewModel { get; set; }
    Option<ParameterState> ParameterState { get; set; }
  }
}
