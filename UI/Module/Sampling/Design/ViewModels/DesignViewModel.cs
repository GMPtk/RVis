using LanguageExt;
using RVis.Data;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static System.String;

namespace Sampling.Design
{
  internal sealed class DesignViewModel : IDesignViewModel
  {
    public int? NSamples { get => 123; set => throw new NotImplementedException(); }
    public int? Seed { get => 456; set => throw new NotImplementedException(); }
    public ICommand GenerateSamples => throw new NotImplementedException();
    public bool CanGenerateSamples { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IDesignActivityViewModel ActivityViewModel { get => Data.SamplesDesignActivityViewModel; set => throw new NotImplementedException(); }

    public DateTime? CreatedOn { get => DateTime.Now; set => throw new NotImplementedException(); }
    public string Invariants { get => Join("; ", Range('A', 'Z').Select(c => $"{c} = {(int)c}")); set => throw new NotImplementedException(); }
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

    public ICommand CreateDesign => throw new NotImplementedException();
    public bool CanCreateDesign { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand AcquireOutputs => throw new NotImplementedException();
    public bool CanAcquireOutputs { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan? EstimatedAcquireDuration { get => TimeSpan.FromSeconds(1234.5678); set => throw new NotImplementedException(); }
    public Arr<(int Index, NumDataTable Output)> Outputs { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ICommand CancelAcquireOutputs => throw new NotImplementedException();
    public bool CanCancelAcquireOutputs { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public double Progress { get => 33.3; set => throw new NotImplementedException(); }

    public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
