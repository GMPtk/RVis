using Google.Protobuf;
using ProtoBuf;
using RVis.Data;
using RVis.Model;
using RVis.R;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static RVis.Base.Check;

namespace RVis.Client
{
  public sealed partial class RVisServer
  {
    private sealed partial class RVisClientProxy : IRVisClient
    {
      internal RVisClientProxy(RVisServer real) =>
        _real = real;

      public int ID => _real.ID;

      public async Task ClearAsync(CancellationToken cancellationToken = default)
      {
        RequireNotNull(_real._rOpsClient);

        var reply = await _real._rOpsClient.ClearGlobalEnvironmentAsync(
          new ClearGlobalEnvironmentRequest(), 
          default, 
          default, 
          cancellationToken
          );

        if (reply.ReplyCase == ClearGlobalEnvironmentReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      public async Task CreateMatrixAsync(
        double[][] source, 
        string objectName,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new CreateMatrixRequest();
        foreach (var array in source)
        {
          var doubleList = new DoubleList();
          doubleList.Doubles.Add(array);
          request.Source.Add(doubleList);
        }
        request.ObjectName = objectName;

        var reply = await _real._rOpsClient.CreateMatrixAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == CreateMatrixReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      public async Task CreateVectorAsync(
        double[] source, 
        string objectName,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new CreateVectorRequest();
        request.Source.Add(source);
        request.ObjectName = objectName;

        var reply = await _real._rOpsClient.CreateVectorAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == CreateVectorReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      public async Task<Dictionary<string, double[]>> EvaluateDoublesAsync(
        string code, 
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new EvaluateDoublesRequest
        {
          Code = code
        };

        var reply = await _real._rOpsClient.EvaluateDoublesAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == EvaluateDoublesReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        return reply.Payload.Doubles.ToDictionary(
          kvp => kvp.Key,
          kvp => kvp.Value.Doubles.ToArray()
          );
      }

      public async Task EvaluateNonQueryAsync(
        string code,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new EvaluateNonQueryRequest
        {
          Code = code
        };

        var reply = await _real._rOpsClient.EvaluateNonQueryAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == EvaluateNonQueryReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      public async Task<NumDataColumn[]> EvaluateNumDataAsync(
        string code,
        CancellationToken cancellationToken = default)
      {
        RequireNotNull(_real._rOpsClient);

        var request = new EvaluateNumDataRequest
        {
          Code = code
        };

        var reply = await _real._rOpsClient.EvaluateNumDataAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == EvaluateNumDataReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        return reply.Payload.DoubleColumns
          .Select(dc => new NumDataColumn(dc.Name, dc.Doubles))
          .ToArray();
      }

      public async Task<Dictionary<string, string[]>> EvaluateStringsAsync(
        string code,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new EvaluateStringsRequest
        {
          Code = code
        };

        var reply = await _real._rOpsClient.EvaluateStringsAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == EvaluateStringsReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        return reply.Payload.Strings.ToDictionary(
          s => s.Key,
          s => s.Value.Strings.ToArray()
          );
      }

      public async Task GarbageCollectAsync(CancellationToken cancellationToken = default)
      {
        RequireNotNull(_real._rOpsClient);

        var reply = await _real._rOpsClient.GarbageCollectAsync(
          new GarbageCollectRequest(),
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == GarbageCollectReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      public async Task<(string Package, string Version)[]> GetInstalledPackagesAsync(
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var reply = await _real._rOpsClient.GetInstalledPackagesAsync(
          new InstalledPackagesRequest(),
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == InstalledPackagesReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        return reply.Payload.InstalledPackages
          .Select(kvp => (kvp.Key, kvp.Value))
          .ToArray();
      }

      public async Task<(string Name, string Value)[]> GetRversionAsync(
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var reply = await _real._rOpsClient.GetRversionAsync(
          new RversionRequest(),
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == RversionReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        return reply.Payload.Rversion
          .Select(kvp => (kvp.Key, kvp.Value))
          .ToArray();
      }

      public async Task<ISymbolInfo[]> InspectSymbolsAsync(
        string pathToCode,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new InspectSymbolsRequest
        {
          PathToCode = pathToCode
        };

        var reply = await _real._rOpsClient.InspectSymbolsAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == InspectSymbolsReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        using var memoryStream = new MemoryStream();
        reply.Payload.SymbolInfos.WriteTo(memoryStream);
        memoryStream.Position = 0;
        var symbolInfos = Serializer.Deserialize<SymbolInfo[]>(memoryStream);

        return symbolInfos;
      }

      public async Task LoadFromBinaryAsync(
        byte[] raw,
        CancellationToken cancellationToken = default)
      {
        RequireNotNull(_real._rOpsClient);

        var request = new LoadFromBinaryRequest
        {
          Raw = ByteString.CopyFrom(raw)
        };

        var reply = await _real._rOpsClient.LoadFromBinaryAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == LoadFromBinaryReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      public async Task RunExecAsync(
        string pathToCode, 
        SimConfig config,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, config);
        memoryStream.Position = 0;

        var request = new RunExecRequest
        {
          PathToCode = pathToCode,
          SimConfig = ByteString.FromStream(memoryStream)
        };

        var reply = await _real._rOpsClient.RunExecAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == RunExecReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      public async Task<byte[]> SaveObjectToBinaryAsync(
        string objectName,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new SaveObjectToBinaryRequest
        {
          ObjectName = objectName
        };

        var reply = await _real._rOpsClient.SaveObjectToBinaryAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == SaveObjectToBinaryReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        return reply.Payload.Binary.ToArray();
      }

      public async Task<byte[]> SerializeAsync(
        string objectName,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new SerializeRequest
        {
          ObjectName = objectName
        };

        var reply = await _real._rOpsClient.SerializeAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == SerializeReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        return reply.Payload.Serialized.ToArray();
      }

      public async Task SourceFileAsync(
        string pathToCode,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new SourceFileRequest
        {
          PathToCode = pathToCode
        };

        var reply = await _real._rOpsClient.SourceFileAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == SourceFileReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      public async Task StopServerAsync(CancellationToken cancellationToken = default)
      {
        RequireNotNull(_real._rOpsClient);

        await _real.StopAsync(cancellationToken);
      }

      public async Task<NumDataTable> TabulateExecOutputAsync(
        SimConfig config,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, config);
        memoryStream.Position = 0;

        var request = new TabulateExecOutputRequest
        {
          SimConfig = ByteString.FromStream(memoryStream)
        };

        var reply = await _real._rOpsClient.TabulateExecOutputAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == TabulateExecOutputReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        var columns = reply.Payload.DoubleColumns
          .Select(dc => new NumDataColumn(dc.Name, dc.Doubles.ToArray()))
          .ToArray();

        return new NumDataTable(config.Title, columns);
      }

      public async Task<NumDataTable> TabulateTmplOutputAsync(
        SimConfig config,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, config);
        memoryStream.Position = 0;

        var request = new TabulateTmplOutputRequest
        {
          SimConfig = ByteString.FromStream(memoryStream)
        };

        var reply = await _real._rOpsClient.TabulateTmplOutputAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == TabulateTmplOutputReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }

        var columns = reply.Payload.DoubleColumns
          .Select(dc => new NumDataColumn(dc.Name, dc.Doubles.ToArray()))
          .ToArray();

        return new NumDataTable(config.Title, columns);
      }

      public async Task UnserializeAsync(
        byte[] raw, 
        string objectName,
        CancellationToken cancellationToken = default
        )
      {
        RequireNotNull(_real._rOpsClient);

        var request = new UnserializeRequest
        {
          Raw = ByteString.CopyFrom(raw),
          ObjectName = objectName
        };

        var reply = await _real._rOpsClient.UnserializeAsync(
          request,
          default,
          default,
          cancellationToken
          );

        if (reply.ReplyCase == UnserializeReply.ReplyOneofCase.Error)
        {
          Throw(reply.Error);
        }
      }

      private static void Throw(Error error)
      {
        var exception = new InvalidOperationException(error.Messages[0]);
        exception.Data["Messages"] = error.Messages.ToArray();
        exception.Data["StackTraces"] = error.StackTraces.ToArray();
        throw exception;
      }

      private readonly RVisServer _real;
    }
  }
}
