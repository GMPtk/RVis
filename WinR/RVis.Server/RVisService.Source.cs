using RVis.Data;
using RVis.Model;
using RVis.ROps;
using System;
using static RVis.Model.SvcRes;

namespace RVis.Server
{
  public partial class RVisService
  {
    public NameStringsMapSvcRes EvaluateStrings(string code)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return ROpsApi.EvaluateStrings(code);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public NameDoublesMapSvcRes EvaluateDoubles(string code)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return ROpsApi.EvaluateDoubles(code);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public NumDataColumnArraySvcRes EvaluateNumData(string code)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return ROpsApi.EvaluateNumData(code);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public UnitSvcRes EvaluateNonQuery(string code)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          ROpsApi.EvaluateNonQuery(code);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }

      return Unit;
    }

    public UnitSvcRes RunExec(string pathToCode, SimConfig config)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          ROpsApi.RunExec(pathToCode, config.SimCode, config.SimInput);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }

      return Unit;
    }

    public UnitSvcRes SourceFile(string pathToCode)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          ROpsApi.SourceFile(pathToCode);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }

      return Unit;
    }

    public NumDataTableSvcRes TabulateExecOutput(SimConfig config)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return new NumDataTable(
            config.Title,
            ROpsApi.TabulateExecOutput(config.SimOutput)
            );
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public NumDataTableSvcRes TabulateTmplOutput(SimConfig config)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return new NumDataTable(
            config.Title,
            ROpsApi.TabulateTmplOutput(config)
            );
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public SymbolInfoArraySvcRes InspectSymbols(string pathToCode)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return ROpsApi.InspectSymbols(pathToCode);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public ByteArraySvcRes Serialize(string objectName)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return ROpsApi.Serialize(objectName);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public UnitSvcRes Unserialize(byte[] raw, string objectName)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          ROpsApi.Unserialize(raw, objectName);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }

      return Unit;
    }

    public ByteArraySvcRes SaveObjectToBinary(string objectName)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          return ROpsApi.SaveObjectToBinary(objectName);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }
    }

    public UnitSvcRes LoadFromBinary(byte[] raw)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          ROpsApi.LoadFromBinary(raw);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }

      return Unit;
    }

    public UnitSvcRes CreateVector(double[] source, string objectName)
    {
      lock (_rvisServiceLock)
      {
        try
        {
          ROpsApi.CreateVector(source, objectName);
        }
        catch (Exception ex)
        {
          _log.Error(ex);
          return ex;
        }
      }

      return Unit;
    }
  }
}
