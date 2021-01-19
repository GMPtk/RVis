using System;
using System.Diagnostics.CodeAnalysis;

namespace RVis.Model
{
  public class SimDataItem<T>
  {
    [AllowNull]
    public T Item { get; }

    public Simulation Simulation { get; }

    public object Requester { get; }

    public object RequestToken { get; }

    public DateTime RequestedOn { get; }

    public DateTime FulfilledOn { get; }

    internal SimDataItem(
      [AllowNull] T item,
      Simulation simulation,
      object requester,
      object requestToken,
      DateTime requestedOn,
      DateTime fulfilledOn
      )
    {
      Item = item;
      Simulation = simulation;
      Requester = requester;
      RequestToken = requestToken;
      RequestedOn = requestedOn;
      FulfilledOn = fulfilledOn;
    }
  }

  internal static class SimDataItem
  {
    internal static SimDataItem<T> Create<T>(
      [AllowNull] T item,
      Simulation simulation,
      object requester,
      object requestToken,
      DateTime requestedOn
      )
      =>
      new SimDataItem<T>(item, simulation, requester, requestToken, requestedOn, default);

    internal static SimDataItem<T> Create<T>(
      T item,
      Simulation simulation,
      object requester,
      object requestToken,
      DateTime requestedOn,
      DateTime fulfilledOn
      )
      =>
      new SimDataItem<T>(item, simulation, requester, requestToken, requestedOn, fulfilledOn);
  }
}
