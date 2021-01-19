using RVis.Model;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public class SimulationViewModel : ISimulationViewModel
  {
    public Simulation Simulation { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public string Title { get; set; }

    public string Description { get; set; }

    public string DirectoryName { get; set; }
  }
}
