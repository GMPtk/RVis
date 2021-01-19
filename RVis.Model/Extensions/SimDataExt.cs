using RVis.Base.Extensions;
using System;

namespace RVis.Model.Extensions
{
  public static class SimDataExt
  {
    public static bool IsResetEvent<T>(this SimDataItem<T> item) =>
      item.RequestToken.Resolve(out SimDataEvent @event) && @event == SimDataEvent.ServiceReset;

    public static bool IsSimDataEvent<T>(this SimDataItem<T> item, out SimDataEvent @event) =>
      item.RequestToken.Resolve(out @event);

    public static string GetDescription<T>(this SimDataItem<T> item)
    {
      if (item.RequestToken is string @string)
      {
        return @string;
      }

      if (item.RequestToken is Enum @enum)
      {
        return @enum.GetDescription() ?? @enum.ToString();
      }

      if (item.Item is OutputRequest) return $"Output request for {item.Simulation.SimConfig.Title}";

      return $"{item.Item?.GetType().Name ?? "?"} for {item.Simulation.SimConfig.Title}";
    }
  }
}
