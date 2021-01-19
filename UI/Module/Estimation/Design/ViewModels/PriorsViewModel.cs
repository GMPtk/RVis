using LanguageExt;
using RVisUI.AppInf;
using System.Collections.ObjectModel;
using static LanguageExt.Prelude;

#nullable disable

namespace Estimation.Design
{
  internal sealed class PriorsViewModel : IPriorsViewModel
  {
    public Arr<IPriorViewModel> AllPriorViewModels => Range(1, 30)
      .Map(i => new PriorViewModel($"Prior{i:0000}", default) { IsSelected = 0 == i % 2 })
      .ToArr<IPriorViewModel>();

    public ObservableCollection<IPriorViewModel> SelectedPriorViewModels
    {
      get => new ObservableCollection<IPriorViewModel>(Range(1, 30)
        .Map(i => new PriorViewModel($"Param{i:0000}", default)
        {
          IsSelected = true,
          Distribution = $"xxx ~ Normal({i},{i * 2})"
        }));
    }

    public int SelectedPriorViewModel { get => 5; set => throw new System.NotImplementedException(); }

    public IParameterDistributionViewModel ParameterDistributionViewModel => new RVisUI.AppInf.Design.ParameterDistributionViewModel();

    public bool IsVisible { get => false; set => throw new System.NotImplementedException(); }
  }
}
