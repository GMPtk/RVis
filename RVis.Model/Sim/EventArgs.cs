using System;

namespace RVis.Model
{
  public class SimulationDeletedEventArgs : EventArgs
  {
    public SimulationDeletedEventArgs(Simulation simulation) =>
      Simulation = simulation;
    
    public Simulation Simulation { get; }
  }
}
