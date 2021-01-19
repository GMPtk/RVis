using ProtoBuf;
using RVis.Data;
using System;
using System.Collections.Generic;

namespace RVis.Model
{
  [ProtoContract]
  public class UnitRes { }

  public static class SvcRes
  {
    public static readonly UnitSvcRes Unit = new UnitSvcRes();

    internal static SvcRes<T> Create<T>(Exception exception) =>
      new SvcRes<T> { Messages = ExceptionToMessages(exception) };

    internal static SvcRes<T> Create<T>(T t) =>
      new SvcRes<T> { Value = t };

    internal static T Create<T, U>(U u) where T : SvcRes<U>, new() =>
      new T { Value = u };

    internal static T Create<T, U>(Exception exception) where T : SvcRes<U>, new() =>
      new T { Messages = ExceptionToMessages(exception) };

    internal static void AssertNoFault(string[] messages)
    {
      if (null == messages) return;
      var exception = new InvalidOperationException(messages[0]);
      exception.Data["Messages"] = messages;
      throw exception;
    }

    private static string[] ExceptionToMessages(Exception exception)
    {
      var messages = new List<string> { exception.Message };

      while (null != exception.InnerException)
      {
        exception = exception.InnerException;
        messages.Add(exception.Message);
      }

      return messages.ToArray();
    }
  }

  [ProtoContract]
  public class NameValueArraySvcRes : SvcRes<(string, string)[]>
  {
    public static implicit operator NameValueArraySvcRes((string, string)[] value) =>
      SvcRes.Create<NameValueArraySvcRes, (string, string)[]>(value);

    public static implicit operator NameValueArraySvcRes(Exception exception) =>
      SvcRes.Create<NameValueArraySvcRes, (string, string)[]>(exception);
  }

  [ProtoContract]
  public class BoolSvcRes : SvcRes<bool>
  {
    public static implicit operator BoolSvcRes(bool value) =>
      SvcRes.Create<BoolSvcRes, bool>(value);

    public static implicit operator BoolSvcRes(Exception exception) =>
      SvcRes.Create<BoolSvcRes, bool>(exception);
  }

  [ProtoContract]
  public class UnitSvcRes : SvcRes<UnitRes>
  {
    public static implicit operator UnitSvcRes(UnitRes value) =>
      SvcRes.Create<UnitSvcRes, UnitRes>(value);

    public static implicit operator UnitSvcRes(Exception exception) =>
      SvcRes.Create<UnitSvcRes, UnitRes>(exception);
  }

  [ProtoContract]
  public class NameDoublesMapSvcRes : SvcRes<Dictionary<string, double[]>>
  {
    public static implicit operator NameDoublesMapSvcRes(Dictionary<string, double[]> value) =>
      SvcRes.Create<NameDoublesMapSvcRes, Dictionary<string, double[]>>(value);

    public static implicit operator NameDoublesMapSvcRes(Exception exception) =>
      SvcRes.Create<NameDoublesMapSvcRes, Dictionary<string, double[]>>(exception);
  }

  [ProtoContract]
  public class NumDataColumnArraySvcRes : SvcRes<NumDataColumn[]>
  {
    public static implicit operator NumDataColumnArraySvcRes(NumDataColumn[] value) =>
      SvcRes.Create<NumDataColumnArraySvcRes, NumDataColumn[]>(value);

    public static implicit operator NumDataColumnArraySvcRes(Exception exception) =>
      SvcRes.Create<NumDataColumnArraySvcRes, NumDataColumn[]>(exception);
  }

  [ProtoContract]
  public class NumDataTableSvcRes : SvcRes<NumDataTable>
  {
    public static implicit operator NumDataTableSvcRes(NumDataTable value) =>
      SvcRes.Create<NumDataTableSvcRes, NumDataTable>(value);

    public static implicit operator NumDataTableSvcRes(Exception exception) =>
      SvcRes.Create<NumDataTableSvcRes, NumDataTable>(exception);
  }

  [ProtoContract]
  public class SymbolInfoArraySvcRes : SvcRes<SymbolInfo[]>
  {
    public static implicit operator SymbolInfoArraySvcRes(SymbolInfo[] value) =>
      SvcRes.Create<SymbolInfoArraySvcRes, SymbolInfo[]>(value);

    public static implicit operator SymbolInfoArraySvcRes(Exception exception) =>
      SvcRes.Create<SymbolInfoArraySvcRes, SymbolInfo[]>(exception);
  }

  [ProtoContract]
  public class ByteArraySvcRes : SvcRes<byte[]>
  {
    public static implicit operator ByteArraySvcRes(byte[] value) =>
      SvcRes.Create<ByteArraySvcRes, byte[]>(value);

    public static implicit operator ByteArraySvcRes(Exception exception) =>
      SvcRes.Create<ByteArraySvcRes, byte[]>(exception);
  }

  [ProtoContract]
  public class NameStringsMapSvcRes : SvcRes<Dictionary<string, string[]>>
  {
    public static implicit operator NameStringsMapSvcRes(Dictionary<string, string[]> value) =>
      SvcRes.Create<NameStringsMapSvcRes, Dictionary<string, string[]>>(value);

    public static implicit operator NameStringsMapSvcRes(Exception exception) =>
      SvcRes.Create<NameStringsMapSvcRes, Dictionary<string, string[]>>(exception);
  }

  [ProtoContract]
  [ProtoInclude(3, typeof(NameValueArraySvcRes))]
  [ProtoInclude(4, typeof(BoolSvcRes))]
  [ProtoInclude(5, typeof(UnitSvcRes))]
  [ProtoInclude(6, typeof(NameDoublesMapSvcRes))]
  [ProtoInclude(7, typeof(NumDataColumnArraySvcRes))]
  [ProtoInclude(8, typeof(NumDataTableSvcRes))]
  [ProtoInclude(9, typeof(SymbolInfoArraySvcRes))]
  [ProtoInclude(10, typeof(ByteArraySvcRes))]
  [ProtoInclude(11, typeof(NameStringsMapSvcRes))]
  public class SvcRes<T>
  {
    [ProtoMember(1)]
    public T Value { get; set; } = default!;

    [ProtoMember(2)]
    public string[] Messages { get; set; } = null!;

    public void Void() =>
      AssertNoFault();

    public T Return()
    {
      AssertNoFault();
      return Value;
    }

    public static implicit operator SvcRes<T>(T t) =>
      SvcRes.Create(t);

    public static implicit operator SvcRes<T>(Exception exception) =>
      SvcRes.Create<T>(exception);

    public static implicit operator T(SvcRes<T> svcRes) =>
      svcRes.Return();

    protected void AssertNoFault() =>
      SvcRes.AssertNoFault(Messages);
  }
}
