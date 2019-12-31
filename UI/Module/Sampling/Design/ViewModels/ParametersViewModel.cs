using LanguageExt;
using RVisUI.AppInf;
using System.Collections.ObjectModel;
using static LanguageExt.Prelude;

namespace Sampling.Design
{
  internal sealed class ParametersViewModel : IParametersViewModel
  {
    public bool IsVisible { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public Arr<IParameterViewModel> AllParameterViewModels => Range(1, 30)
      .Map(i => new ParameterViewModel($"Param{i:0000}", default) { IsSelected = 0 == i % 2 })
      .ToArr<IParameterViewModel>();

    public ObservableCollection<IParameterViewModel> SelectedParameterViewModels
    {
      get => new ObservableCollection<IParameterViewModel>(Range(1, 30)
        .Map(i => new ParameterViewModel($"Param{i:0000}", default)
        {
          IsSelected = true,
          Distribution = $"xxx ~ Normal({i},{i * 2})"
        }));
    }

    public int SelectedParameterViewModel { get => 5; set => throw new System.NotImplementedException(); }

    public IParameterDistributionViewModel ParameterDistributionViewModel => new RVisUI.AppInf.Design.ParameterDistributionViewModel();
  }
}
