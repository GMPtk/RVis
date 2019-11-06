using System;
using System.ServiceModel;

namespace RVis
{
  public static class ServiceHelper
  {
    public static NetNamedPipeBinding MakeNetNamedPipeBinding() => 
      new NetNamedPipeBinding()
      {
        MaxReceivedMessageSize = 100000000,
        MaxBufferSize = 100000000,
        SendTimeout = TimeSpan.FromMinutes(10),
        ReceiveTimeout = TimeSpan.FromHours(9)
      };

    public static string GetRVisServiceNamedPipeAddress(string id) => 
      $"net.pipe://localhost/rvis{id}/service";
  }
}
