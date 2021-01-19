using ReactiveUI;
using RVis.Model;
using System.IO;

namespace RVisUI.Mvvm
{
  public class SimulationViewModel : ReactiveObject, ISimulationViewModel
  {
    public SimulationViewModel(Simulation simulation)
    {
      Simulation = simulation;
      Title = simulation.SimConfig.Title;
      Description = simulation.SimConfig.Description;
      DirectoryName = new DirectoryInfo(simulation.PathToSimulation).Name;
    }

    public Simulation Simulation { get; }
    public string Title { get; }
    public string? Description { get; }
    public string DirectoryName { get; }
  }
}
