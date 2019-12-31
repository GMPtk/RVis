using LanguageExt;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sampling.Design
{
  internal sealed class ViewCorrelationViewModel : IViewCorrelationViewModel
  {
    private const int N_PARAMETERS = 10;

    public Arr<string> ParameterNames => Range(1, N_PARAMETERS).Map(i => $"Parameter {i:0000}").ToArr();

    public IRCParameterViewModel[][] RCParameterViewModels
    {
      get
      {
        var rcParameterViewModels = new IRCParameterViewModel[N_PARAMETERS][];

        Range(0, N_PARAMETERS).Iter(i =>
        {
          rcParameterViewModels[i] = new IRCParameterViewModel[N_PARAMETERS];

          Range(0, N_PARAMETERS).Iter(j =>
          {
            if (j == i)
            {
              rcParameterViewModels[i][j] = new RCParameterDiagonalViewModel(ParameterNames[i], (i + 1) * (j + 1));
            }
            else
            {
              rcParameterViewModels[i][j] = new RCParameterViewModel(ParameterNames[i], default)
              {
                CorrelationN = (i + 1d) * 1000d + (j + 1d)
              };
            }
          });
        });

        return rcParameterViewModels;
      }
    }

    public ICommand Close => throw new NotImplementedException();

    public bool? DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
