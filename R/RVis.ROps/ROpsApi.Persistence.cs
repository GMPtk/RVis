using RDotNet;
using static System.String;
using static RVis.ROps.Properties.Resources;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    public static byte[] Serialize(string objectName)
    {
      var instance = REngine.GetInstance();
      var rawVector = instance.Evaluate($"serialize({objectName}, NULL)");
      return rawVector.AsRaw().ToArray();
    }

    public static void Unserialize(byte[] raw, string objectName)
    {
      var instance = REngine.GetInstance();
      var rawVectorName = $"{objectName}_as_raw";
      instance.SetSymbol(rawVectorName, instance.CreateRawVector(raw));
      instance.Evaluate($"{objectName} <- unserialize({rawVectorName})");
    }

    public static byte[] SaveObjectToBinary(string objectName)
    {
      var instance = REngine.GetInstance();
      var code = Format(FMT_SAVE_OBJECT_TO_BINARY, objectName);
      var rawVector = instance.Evaluate(code);
      return rawVector.AsRaw().ToArray();
    }

    public static void LoadFromBinary(byte[] raw)
    {
      var instance = REngine.GetInstance();
      var rawVectorName = $"rawLFB";
      instance.SetSymbol(rawVectorName, instance.CreateRawVector(raw));
      instance.Evaluate(FMT_LOAD_FROM_BINARY);
    }
  }
}
