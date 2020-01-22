using System.Collections.Generic;

namespace RVis.Model
{
  internal class ServerSlot
  {
    internal ServerSlot(int id, IRVisServer server)
    {
      ID = id;
      Server = server;
    }

    internal int ID { get; }
    
    internal IRVisServer Server { get; }
    
    internal IDictionary<Simulation, MCSimExecutor> MCSimExecutors { get; } 
      = new Dictionary<Simulation, MCSimExecutor>();
    
    internal bool IsFree { get; set; }
  }
}
