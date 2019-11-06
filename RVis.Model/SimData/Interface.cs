using LanguageExt;
using RVis.Data;
using System;

namespace RVis.Model
{
  public interface ISimData
  {
    IObservable<SimDataItem<OutputRequest>> OutputRequests { get; }
    bool HasOutput(string serieInputHash, Simulation simulation);
    bool HasOutput(SimInput serieInput, Simulation simulation);
    Option<NumDataTable> GetOutput(string serieInputHash, Simulation simulation);
    Option<NumDataTable> GetOutput(SimInput serieInput, Simulation simulation);
    Option<(SimInput SerieInput, OutputOrigin OutputOrigin, bool Persist, DateTime PersistedOn)> GetOutputInfo(string serieInputHash, Simulation simulation);
    Option<(SimInput SerieInput, OutputOrigin OutputOrigin, bool Persist, DateTime PersistedOn)> GetOutputInfo(SimInput serieInput, Simulation simulation);
    bool RequestOutput(Simulation simulation, SimInput seriesInput, object requester, object requestToken, bool persist);
    void Clear(bool includePendingRequests);
    Option<(int ms, int n)> GetExecutionInterval(Simulation simulation);
  }
}
