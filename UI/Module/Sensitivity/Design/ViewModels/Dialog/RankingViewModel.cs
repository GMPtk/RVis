using LanguageExt;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sensitivity.Design
{
  internal sealed class RankingViewModel : IRankingViewModel
  {
    public double? From
    {
      get => 1.23;
      set => throw new NotImplementedException();
    }

    public double? To
    {
      get => 4.56;
      set => throw new NotImplementedException();
    }

    public string XUnits => "x units x units x units x units";

    public Arr<IOutputViewModel> OutputViewModels => Range(1, 20)
      .Map(i => new OutputViewModel($"output {i:0000}") { IsSelected = i % 2 == 0 })
      .ToArr<IOutputViewModel>();

    public Arr<IRankedParameterViewModel> RankedParameterViewModels => Range(1, 20)
      .Map(i => new RankedParameterViewModel($"parameter {i:0000}", i * i) { IsSelected = i % 2 == 0 })
      .ToArr<IRankedParameterViewModel>();

    public bool CanOK => throw new NotImplementedException();

    public ICommand OK => throw new NotImplementedException();

    public ICommand Cancel => throw new NotImplementedException();

    public bool? DialogResult
    {
      get => throw new NotImplementedException();
      set => throw new NotImplementedException();
    }
  }
}
