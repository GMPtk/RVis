using LanguageExt;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;

#nullable disable

namespace Estimation.Design
{
  internal sealed class OutputErrorViewModel : IOutputErrorViewModel
  {
    public Arr<string> ErrorModelNames => Array("Normal", "Log normal");
    public int SelectedErrorModelName { get => 1; set => throw new NotImplementedException(); }
    public IErrorViewModel ErrorViewModel
    {
      get => new HeteroscedasticExpErrorViewModel()
      {
        Variable = "x",
        Sigma = 0.6
      };
      set => throw new NotImplementedException();
    }
    public Option<OutputState> OutputState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
