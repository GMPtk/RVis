using LanguageExt;
using ReactiveUI;
using RVis.Model;

namespace RVisUI.Mvvm
{
  public sealed class DrugXNotSoSimpleAcatViewModel : ReactiveObject, IDrugXNotSoSimpleAcatViewModel
  {
    public string Name => "Drug X (not so simple)";

    public bool CanConfigureSimulation
    {
      get => _canConfigureSimulation;
      set => this.RaiseAndSetIfChanged(ref _canConfigureSimulation, value);
    }
    private bool _canConfigureSimulation;

    public Option<Simulation> ConfigureSimulation(bool import)
    {
      throw new System.NotImplementedException();
    }

    public bool IsReady => true;

    public void Load(string pathToAcat)
    {
    }
  }
}
