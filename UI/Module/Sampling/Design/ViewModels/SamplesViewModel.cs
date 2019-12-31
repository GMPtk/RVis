using LanguageExt;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sampling.Design
{
  internal sealed class SamplesViewModel : ISamplesViewModel
  {
    public bool IsReadOnly { get => true; set => throw new NotImplementedException(); }

    public Arr<string> Distributions => Range(1, 30).Map(i => $"Var{i:000} ~ dist{i:000}(dist params...)").ToArr();

    public Arr<string> Invariants => Range(1, 30).Map(i => $"Var{i:000} = {i}").ToArr();

    public int? NSamples { get => 123; set => throw new NotImplementedException(); }
    public int? Seed { get => 456; set => throw new NotImplementedException(); }

    public LatinHypercubeDesignType LatinHypercubeDesignType => LatinHypercubeDesignType.Centered;

    public ICommand ConfigureLHS => throw new NotImplementedException();

    public bool CanConfigureLHS => throw new NotImplementedException();

    public RankCorrelationDesignType RankCorrelationDesignType => RankCorrelationDesignType.ImanConn;

    public ICommand ConfigureRC => throw new NotImplementedException();

    public bool CanConfigureRC => throw new NotImplementedException();

    public ICommand GenerateSamples => throw new NotImplementedException();

    public bool CanGenerateSamples => throw new NotImplementedException();

    public DataView Samples => Data.Samples;

    public ICommand ViewCorrelation => throw new NotImplementedException();

    public bool CanViewCorrelation => throw new NotImplementedException();

    public ObservableCollection<IParameterSamplingViewModel> ParameterSamplingViewModels =>
      new ObservableCollection<IParameterSamplingViewModel>(
        new IParameterSamplingViewModel[]
        {
          Data.BetaParameterSamplingViewModel,
          Data.BetaScaledParameterSamplingViewModel,
          Data.GammaParameterSamplingViewModel,
          Data.InverseGammaParameterSamplingViewModel,
          Data.LogNormalParameterSamplingViewModel,
          Data.LogUniformParameterSamplingViewModel,
          Data.NormalParameterSamplingViewModel,
          Data.StudentTParameterSamplingViewModel,
          Data.UniformParameterSamplingViewModel
        });
  }
}
