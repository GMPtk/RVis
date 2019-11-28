using RVis.Data;
using System;

namespace RVis.Model
{
  public sealed partial class SimData
  {
    internal class SimDataOutput
    {
      internal static SimDataOutput Create(
        Simulation simulation, 
        SimInput serieInput, 
        NumDataTable serie, 
        OutputOrigin outputOrigin, 
        DateTime acquiredOn, 
        bool persist
        ) =>
        new SimDataOutput(simulation, serieInput, serie, outputOrigin, acquiredOn, persist, default);

      internal SimDataOutput ToPersisted(DateTime persistedOn) =>
        new SimDataOutput(Simulation, SerieInput, Serie, OutputOrigin, AcquiredOn, Persist, persistedOn);

      private SimDataOutput(
        Simulation simulation, 
        SimInput serieInput, 
        NumDataTable serie, 
        OutputOrigin outputOrigin, 
        DateTime acquiredOn, 
        bool persist, 
        DateTime persistedOn
        )
      {
        Simulation = simulation;
        SerieInput = serieInput;
        Serie = serie;
        OutputOrigin = outputOrigin;
        AcquiredOn = acquiredOn;
        Persist = persist;
        PersistedOn = persistedOn;
      }

      internal Simulation Simulation { get; }
      internal SimInput SerieInput { get; }
      internal NumDataTable Serie { get; }
      internal OutputOrigin OutputOrigin { get; }
      internal DateTime AcquiredOn { get; }
      internal bool Persist { get; }
      internal DateTime PersistedOn { get; }
    }
  }
}