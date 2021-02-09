using LanguageExt;
using RVis.Model;
using System;

namespace RVisUI.Mvvm.Design
{
  public sealed class DrugXNotSoSimpleAcatViewModel : IDrugXNotSoSimpleAcatViewModel
  {
    public string Name => "Drug! Not so simple!";

    public bool CanConfigureSimulation => throw new NotImplementedException();

    public bool IsReady => true;

    public Option<Simulation> ConfigureSimulation(bool import)
    {
      throw new NotImplementedException();
    }

    public void Load(string pathToAcat)
    {
      throw new NotImplementedException();
    }
  }
}
