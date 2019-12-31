using LanguageExt;
using System;
using System.Linq;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static System.String;
using static System.Linq.Enumerable;

namespace Sampling.Design
{
  internal class LHSConfigurationViewModel : ILHSConfigurationViewModel
  {
    public bool IsDiceDesignInstalled => true;

    public LatinHypercubeDesignType LatinHypercubeDesignType { get => LatinHypercubeDesignType.Centered; set => throw new NotImplementedException(); }
    public bool UseSimulatedAnnealing { get => true; set => throw new NotImplementedException(); }

    public double? T0 { get => 123; set => throw new NotImplementedException(); }
    public double? C { get => 234; set => throw new NotImplementedException(); }
    public int? Iterations { get => 345; set => throw new NotImplementedException(); }
    public double? P { get => 456; set => throw new NotImplementedException(); }
    public TemperatureDownProfile Profile { get => TemperatureDownProfile.GeometricalMorris; set => throw new NotImplementedException(); }
    public int? Imax { get => 567; set => throw new NotImplementedException(); }

    public ICommand Disable => throw new NotImplementedException();

    public bool CanOK => throw new NotImplementedException();

    public ICommand OK => throw new NotImplementedException();

    public ICommand Cancel => throw new NotImplementedException();

    public bool? DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public LatinHypercubeDesign LatinHypercubeDesign { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
