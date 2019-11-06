using LanguageExt;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static System.Globalization.CultureInfo;

namespace Estimation.Design
{
  internal sealed class DesignViewModel : IDesignViewModel
  {
    public int? Iterations { get => 123; set => throw new NotImplementedException(); }

    public Arr<string> Priors => Range(1, 30)
      .Map(i => $"Prior{i:0000}")
      .ToArr();

    public Arr<string> Invariants => Range(1, 30)
      .Map(i => $"Invariant{i:0000}")
      .ToArr();

    public Arr<string> Outputs => Range(1, 30)
      .Map(i => $"Output{i:0000}")
      .ToArr();

    public Arr<string> Observations => Range(1, 30)
      .Map(i => $"Observations{i:0000}")
      .ToArr();

    public int? BurnIn { get => 456; set => throw new NotImplementedException(); }

    public Arr<int> ChainsOptions => Range(1, 7).ToArr();

    public int ChainsIndex { get => 3; set => throw new NotImplementedException(); }

    public ICommand CreateDesign => throw new NotImplementedException();

    public bool CanCreateDesign => false;

    public DateTime? DesignCreatedOn => DateTime.Now;

    public ICommand UnloadDesign => throw new NotImplementedException();

    public bool CanUnloadDesign => false;

    public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
