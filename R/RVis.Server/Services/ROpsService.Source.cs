using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProtoBuf;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.R;
using RVis.ROps;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static RVis.R.ROps;

namespace RVis.Server
{
  public partial class ROpsService : ROpsBase
  {
    public override async Task<EvaluateStringsReply> EvaluateStrings(EvaluateStringsRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new EvaluateStringsReply();
      try
      {
        var strings = ROpsApi.EvaluateStrings(request.Code);

        reply.Payload = new EvaluateStringsPayload();

        foreach (var kvp in strings)
        {
          var stringList = new StringList();
          if (kvp.Value.IsCollection())
          {
            stringList.Strings.Add(kvp.Value);
          }
          reply.Payload.Strings.Add(kvp.Key, stringList);
        }
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(EvaluateStrings));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<EvaluateDoublesReply> EvaluateDoubles(EvaluateDoublesRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new EvaluateDoublesReply();
      try
      {
        var doubles = ROpsApi.EvaluateDoubles(request.Code);

        reply.Payload = new EvaluateDoublesPayload();

        foreach (var kvp in doubles)
        {
          var doubleList = new DoubleList();
          if (kvp.Value.IsCollection())
          {
            doubleList.Doubles.Add(kvp.Value);
          }
          reply.Payload.Doubles.Add(kvp.Key, doubleList);
        }
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(EvaluateDoubles));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<EvaluateNumDataReply> EvaluateNumData(EvaluateNumDataRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new EvaluateNumDataReply();
      try
      {
        var numDataColumns = ROpsApi.EvaluateNumData(request.Code);

        reply.Payload = new EvaluateNumDataPayload();

        foreach (var numDataColumn in numDataColumns)
        {
          var doubleColumn = new DoubleColumn
          {
            Name = numDataColumn.Name
          };
          doubleColumn.Doubles.Add(numDataColumn.Data);
          reply.Payload.DoubleColumns.Add(doubleColumn);
        }
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(EvaluateNumData));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<EvaluateNonQueryReply> EvaluateNonQuery(EvaluateNonQueryRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new EvaluateNonQueryReply();
      try
      {
        ROpsApi.EvaluateNonQuery(request.Code);

        reply.Payload = new EvaluateNonQueryPayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(EvaluateNonQuery));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<RunExecReply> RunExec(RunExecRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new RunExecReply();
      try
      {
        using var memoryStream = new MemoryStream(request.SimConfig.Length);
        request.SimConfig.WriteTo(memoryStream);
        memoryStream.Position = 0;
        var simConfig = Serializer.Deserialize<SimConfig>(memoryStream);

        ROpsApi.RunExec(request.PathToCode, simConfig.SimCode, simConfig.SimInput);

        reply.Payload = new RunExecPayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(RunExec));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<SourceFileReply> SourceFile(SourceFileRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new SourceFileReply();
      try
      {
        ROpsApi.SourceFile(request.PathToCode);

        reply.Payload = new SourceFilePayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(RunExec));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<TabulateExecOutputReply> TabulateExecOutput(TabulateExecOutputRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new TabulateExecOutputReply();
      try
      {
        using var memoryStream = new MemoryStream(request.SimConfig.Length);
        request.SimConfig.WriteTo(memoryStream);
        memoryStream.Position = 0;
        var simConfig = Serializer.Deserialize<SimConfig>(memoryStream);

        var numDataColumns = ROpsApi.TabulateExecOutput(simConfig.SimOutput);

        reply.Payload = new TabulateExecOutputPayload();

        foreach (var numDataColumn in numDataColumns)
        {
          var doubleColumn = new DoubleColumn
          {
            Name = numDataColumn.Name
          };
          doubleColumn.Doubles.Add(numDataColumn.Data);
          reply.Payload.DoubleColumns.Add(doubleColumn);
        }
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(TabulateExecOutput));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<TabulateTmplOutputReply> TabulateTmplOutput(TabulateTmplOutputRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new TabulateTmplOutputReply();
      try
      {
        using var memoryStream = new MemoryStream(request.SimConfig.Length);
        request.SimConfig.WriteTo(memoryStream);
        memoryStream.Position = 0;
        var simConfig = Serializer.Deserialize<SimConfig>(memoryStream);

        var numDataColumns = ROpsApi.TabulateTmplOutput(simConfig);

        reply.Payload = new TabulateTmplOutputPayload();

        foreach (var numDataColumn in numDataColumns)
        {
          var doubleColumn = new DoubleColumn
          {
            Name = numDataColumn.Name
          };
          doubleColumn.Doubles.Add(numDataColumn.Data);
          reply.Payload.DoubleColumns.Add(doubleColumn);
        }
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(TabulateTmplOutput));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<InspectSymbolsReply> InspectSymbols(InspectSymbolsRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new InspectSymbolsReply();
      try
      {
        var symbolInfos = ROpsApi.InspectSymbols(request.PathToCode);

        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, symbolInfos);
        memoryStream.Position = 0;

        reply.Payload = new InspectSymbolsPayload
        {
          SymbolInfos = ByteString.FromStream(memoryStream)
        };
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(InspectSymbols));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<SerializeReply> Serialize(SerializeRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new SerializeReply();
      try
      {
        var serialized = ROpsApi.Serialize(request.ObjectName);

        reply.Payload = new SerializePayload
        {
          Serialized = ByteString.CopyFrom(serialized)
        };
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(Serialize));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<UnserializeReply> Unserialize(UnserializeRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new UnserializeReply();
      try
      {
        ROpsApi.Unserialize(request.Raw.ToByteArray(), request.ObjectName);

        reply.Payload = new UnserializePayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(Unserialize));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<SaveObjectToBinaryReply> SaveObjectToBinary(SaveObjectToBinaryRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new SaveObjectToBinaryReply();
      try
      {
        var binary = ROpsApi.SaveObjectToBinary(request.ObjectName);

        reply.Payload = new SaveObjectToBinaryPayload
        {
          Binary = ByteString.CopyFrom(binary)
        };
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(SaveObjectToBinary));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<LoadFromBinaryReply> LoadFromBinary(LoadFromBinaryRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new LoadFromBinaryReply();
      try
      {
        ROpsApi.LoadFromBinary(request.Raw.ToArray());

        reply.Payload = new LoadFromBinaryPayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(LoadFromBinary));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<CreateVectorReply> CreateVector(CreateVectorRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new CreateVectorReply();
      try
      {
        var source = request.Source.ToArray();
        ROpsApi.CreateVector(source, request.ObjectName);

        reply.Payload = new CreateVectorPayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(CreateVector));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }

    public override async Task<CreateMatrixReply> CreateMatrix(CreateMatrixRequest request, ServerCallContext context)
    {
      await _semaphoreSlim.WaitAsync();

      var reply = new CreateMatrixReply();
      try
      {
        var source = request.Source.Select(dl => dl.Doubles.ToArray()).ToArray();
        ROpsApi.CreateMatrix(source.ToMultidimensional(), request.ObjectName);

        reply.Payload = new CreateMatrixPayload();
      }
      catch (Exception ex)
      {
        reply.Error = PopulateError(ex);
        _logger.LogError(ex, nameof(CreateMatrix));
      }
      finally
      {
        _semaphoreSlim.Release();
      }

      return await Task.FromResult(reply);
    }
  }
}
