using RVis.Data;
using RVis.Model;
using System.Collections.Generic;
using static RVis.Base.Check;

namespace RVis.Client
{
  public sealed partial class RVisServer
  {
    private sealed partial class RVisClientProxy
    {
      public Dictionary<string, string[]> EvaluateStrings(string code)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.EvaluateStrings(code);

          return svcRes.Return();
        }
      }

      public Dictionary<string, double[]> EvaluateDoubles(string code)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.EvaluateDoubles(code);

          return svcRes.Return();
        }
      }

      public NumDataColumn[] EvaluateNumData(string code)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.EvaluateNumData(code);

          return svcRes.Return();
        }
      }

      public void EvaluateNonQuery(string code)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.EvaluateNonQuery(code);

          svcRes.Void();
        }
      }

      public void RunExec(string pathToCode, SimConfig config)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.RunExec(pathToCode, config);

          svcRes.Void();
        }
      }

      public void SourceFile(string pathToCode)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.SourceFile(pathToCode);

          svcRes.Void();
        }
      }

      public NumDataTable TabulateExecOutput(SimConfig config)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          return _real._service.TabulateExecOutput(config);
        }
      }

      public NumDataTable TabulateTmplOutput(SimConfig config)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          return _real._service.TabulateTmplOutput(config);
        }
      }

      public ISymbolInfo[] InspectSymbols(string pathToCode)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          return _real._service.InspectSymbols(pathToCode);
        }
      }

      public byte[] Serialize(string objectName)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          return _real._service.Serialize(objectName);
        }
      }

      public void Unserialize(byte[] raw, string objectName)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.Unserialize(raw, objectName);

          svcRes.Void();
        }
      }

      public byte[] SaveObjectToBinary(string objectName)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          return _real._service.SaveObjectToBinary(objectName);
        }
      }

      public void LoadFromBinary(byte[] raw)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.LoadFromBinary(raw);

          svcRes.Void();
        }
      }

      public void CreateVector(double[] source, string objectName)
      {
        lock (_real._serviceLock)
        {
          RequireFalse(_real._service.IsBusy(), "Call in progress");

          var svcRes = _real._service.CreateVector(source, objectName);

          svcRes.Void();
        }
      }
    }
  }
}
