using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sampling.Design
{
  internal sealed class OutputsSelectedSampleViewModel : IOutputsSelectedSampleViewModel
  {
    public int SelectedSample { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public string SampleIdentifier => "Sample #123456789";

    public Arr<string> ParameterValues => Range(1, 40)
      .Map(i => $"param{i:0000} = {i * 2d} [u]")
      .ToArr();

    public ICommand ShareParameterValues => throw new NotImplementedException();
  }
}
