using LanguageExt;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sampling.Design
{
  internal sealed class RCConfigurationViewModel : IRCConfigurationViewModel
  {
    public bool IsMc2dInstalled => true;

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
            if (j < i)
            {
              rcParameterViewModels[i][j] = new RCParameterMirrorViewModel(rcParameterViewModels[j][i]);
            }
            else if (j == i)
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

    public string TargetParameterV => ParameterNames[0];

    public string TargetParameterH => ParameterNames[1];

    public Arr<double> TargetCorrelations => Range(-100, 201).Map(i => i / 100d).ToArr();

    public RankCorrelationDesignType RankCorrelationDesignType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand Disable => throw new NotImplementedException();

    public bool CanOK => throw new NotImplementedException();

    public ICommand OK => throw new NotImplementedException();

    public ICommand Cancel => throw new NotImplementedException();

    public bool? DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Arr<(string Parameter, Arr<double> Correlations)> Correlations { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
