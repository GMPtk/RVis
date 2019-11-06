namespace RVis.Model
{
  internal class ServerSlot
  {
    internal ServerSlot(IRVisServer server) => Server = server;
    internal IRVisServer Server { get; }
    internal bool IsFree { get; set; }
  }
}
