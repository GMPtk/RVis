using LanguageExt;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sampling.Design
{
  internal sealed class SamplesViewModel : ISamplesViewModel
  {
    public bool IsReadOnly { get => true; set => throw new NotImplementedException(); }

    public Arr<string> Distributions => Range(1, 30).Map(i => $"Var{i:000} ~ dist{i:000}(dist params...)").ToArr();

    public ICommand ShareParameters => throw new NotImplementedException();

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

    public string[][]? Statistics => Range(1, 100).Map(i => new[] { 
      $"ALongLongParamName{i:000}", 
      $"{1.234E+01*i}", 
      $"{2.345E+02*i}", 
      $"{3.456E+03*i} - {4.567E+04*i}", 
      $"{5.678E+05*i} - {6.789E+06*i}", 
      $"{7.890E+07*i} - {8.901E+08*i}" 
    }).ToArray();

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
