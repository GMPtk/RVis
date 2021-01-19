using LanguageExt;
using RVis.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RVis.Model
{
  public interface IRVisServerPool
  {
    Option<ServerLicense> RequestServer();

    Option<ServerLicense> RenewServerLicense(ServerLicense serverLicense);

    WaitHandle SlotFree { get; }

    IObservable<(ServerLicense ServerLicense, bool HasExpired)> ServerLicenses { get; }
  }

  public interface IRVisServer
  {
    int ID { get; }

    bool IsUp { get; }

    Task<IRVisClient> OpenChannelAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
  }

  public interface IRVisClient
  {
    int ID { get; }

    Task ClearAsync(CancellationToken cancellationToken = default);
    Task CreateMatrixAsync(double[][] source, string objectName, CancellationToken cancellationToken = default);
    Task CreateVectorAsync(double[] source, string objectName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, double[]>> EvaluateDoublesAsync(string code, CancellationToken cancellationToken = default);
    Task EvaluateNonQueryAsync(string code, CancellationToken cancellationToken = default);
    Task<NumDataColumn[]> EvaluateNumDataAsync(string code, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string[]>> EvaluateStringsAsync(string code, CancellationToken cancellationToken = default);
    Task GarbageCollectAsync(CancellationToken cancellationToken = default);
    Task<(string Package, string Version)[]> GetInstalledPackagesAsync(CancellationToken cancellationToken = default);
    Task<(string Name, string Value)[]> GetRversionAsync(CancellationToken cancellationToken = default);
    Task<ISymbolInfo[]> InspectSymbolsAsync(string pathToCode, CancellationToken cancellationToken = default);
    Task LoadFromBinaryAsync(byte[] raw, CancellationToken cancellationToken = default);
    Task RunExecAsync(string pathToCode, SimConfig config, CancellationToken cancellationToken = default);
    Task<byte[]> SaveObjectToBinaryAsync(string objectName, CancellationToken cancellationToken = default);
    Task<byte[]> SerializeAsync(string objectName, CancellationToken cancellationToken = default);
    Task SourceFileAsync(string pathToCode, CancellationToken cancellationToken = default);
    Task StopServerAsync(CancellationToken cancellationToken = default);
    Task<NumDataTable> TabulateExecOutputAsync(SimConfig config, CancellationToken cancellationToken = default);
    Task<NumDataTable> TabulateTmplOutputAsync(SimConfig config, CancellationToken cancellationToken = default);
    Task UnserializeAsync(byte[] raw, string objectName, CancellationToken cancellationToken = default);
  }

  public interface IRVisServiceCallback
  {
    //[OperationContract(IsOneWay = true)]
    //void NotifyResult(ResultType resultType);

    void NotifyGeneratedOutput(NumDataTable[] generatedOutput);

    void NotifyFault(string message, string innerMessage);
  }

  public interface IRVisService
  {
    NameValueArraySvcRes GetRversion();

    NameValueArraySvcRes GetInstalledPackages();

    BoolSvcRes IsBusy();

    UnitSvcRes Clear();

    UnitSvcRes GarbageCollect();

    UnitSvcRes Shutdown();

    NameStringsMapSvcRes EvaluateStrings(string code);

    NameDoublesMapSvcRes EvaluateDoubles(string code);

    NumDataColumnArraySvcRes EvaluateNumData(string code);

    UnitSvcRes EvaluateNonQuery(string code);

    UnitSvcRes RunExec(string pathToCode, SimConfig config);

    UnitSvcRes SourceFile(string pathToCode);

    NumDataTableSvcRes TabulateExecOutput(SimConfig config);

    NumDataTableSvcRes TabulateTmplOutput(SimConfig config);

    SymbolInfoArraySvcRes InspectSymbols(string pathToCode);

    ByteArraySvcRes Serialize(string objectName);

    UnitSvcRes Unserialize(byte[] raw, string objectName);

    ByteArraySvcRes SaveObjectToBinary(string objectName);

    UnitSvcRes LoadFromBinary(byte[] raw);

    UnitSvcRes CreateVector(double[] source, string objectName);

    UnitSvcRes CreateMatrix(double[][] source, string objectName);
  }
}
