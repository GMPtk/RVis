using System;

namespace RVis.Model
{
  public sealed partial class SimData
  {
    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _executionIntervals.Values.Iter(v => v.Save());
          if (_outputRequestsSubject is IDisposable outputRequestsSubject) outputRequestsSubject.Dispose();
          _ctsDataService?.Cancel();
          _ctsDataService?.Dispose();
          _mreDataService.Dispose();
          _outputRequests.Clear();
          _outputs.Clear();
        }

        _disposed = true;
      }
    }

    private bool _disposed = false;
  }
}
