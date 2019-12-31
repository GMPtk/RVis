using LanguageExt;
using RVis.Data;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;

namespace RVis.Model
{
  public interface IRVisServerPool
  {
    Option<ServerLicense> RequestServer();

    Option<ServerLicense> RenewServerLicense(ServerLicense serverLicense);

    WaitHandle SlotFree { get; }

    IObservable<(int ServerID, ServerLicense ServerLicense, bool HasExpired)> ServerLicenses { get; }
  }

  public interface IRVisServer
  {
    int ID { get; }

    IRVisClient OpenChannel();

    bool IsUp { get; }

    void Stop();
  }

  public interface IRVisClient : IDisposable
  {
    int ID { get; }

    (string Name, string Value)[] GetRversion();

    (string Package, string Version)[] GetInstalledPackages();

    void Clear();

    void GarbageCollect();

    void StopServer();

    Dictionary<string, string[]> EvaluateStrings(string code);

    Dictionary<string, double[]> EvaluateDoubles(string code);

    NumDataColumn[] EvaluateNumData(string code);

    void EvaluateNonQuery(string code);

    void RunExec(string pathToCode, SimConfig config);

    void SourceFile(string pathToCode);

    NumDataTable TabulateExecOutput(SimConfig config);

    NumDataTable TabulateTmplOutput(SimConfig config);

    ISymbolInfo[] InspectSymbols(string pathToCode);

    byte[] Serialize(string objectName);

    void Unserialize(byte[] raw, string objectName);

    byte[] SaveObjectToBinary(string objectName);

    void LoadFromBinary(byte[] raw);

    void CreateVector(double[] source, string objectName);

    void CreateMatrix(double[][] source, string objectName);
  }

  public interface IRVisServiceCallback
  {
    //[OperationContract(IsOneWay = true)]
    //void NotifyResult(ResultType resultType);

    [OperationContract]
    void NotifyGeneratedOutput(NumDataTable[] generatedOutput);

    [OperationContract]
    void NotifyFault(string message, string innerMessage);
  }

  [ServiceContract(CallbackContract = typeof(IRVisServiceCallback))]
  public interface IRVisService
  {
    [OperationContract]
    NameValueArraySvcRes GetRversion();

    [OperationContract]
    NameValueArraySvcRes GetInstalledPackages();

    [OperationContract]
    BoolSvcRes IsBusy();

    [OperationContract]
    UnitSvcRes Clear();

    [OperationContract]
    UnitSvcRes GarbageCollect();

    [OperationContract]
    UnitSvcRes Shutdown();

    [OperationContract]
    NameStringsMapSvcRes EvaluateStrings(string code);

    [OperationContract]
    NameDoublesMapSvcRes EvaluateDoubles(string code);

    [OperationContract]
    NumDataColumnArraySvcRes EvaluateNumData(string code);

    [OperationContract]
    UnitSvcRes EvaluateNonQuery(string code);

    [OperationContract]
    UnitSvcRes RunExec(string pathToCode, SimConfig config);

    [OperationContract]
    UnitSvcRes SourceFile(string pathToCode);

    [OperationContract]
    NumDataTableSvcRes TabulateExecOutput(SimConfig config);

    [OperationContract]
    NumDataTableSvcRes TabulateTmplOutput(SimConfig config);

    [OperationContract]
    SymbolInfoArraySvcRes InspectSymbols(string pathToCode);

    [OperationContract]
    ByteArraySvcRes Serialize(string objectName);

    [OperationContract]
    UnitSvcRes Unserialize(byte[] raw, string objectName);

    [OperationContract]
    ByteArraySvcRes SaveObjectToBinary(string objectName);

    [OperationContract]
    UnitSvcRes LoadFromBinary(byte[] raw);

    [OperationContract]
    UnitSvcRes CreateVector(double[] source, string objectName);

    [OperationContract]
    UnitSvcRes CreateMatrix(double[][] source, string objectName);
  }
}
