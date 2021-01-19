using RDotNet;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    public static void CreateVector(double[] source, string objectName)
    {
      var instance = REngine.GetInstance();
      instance.SetSymbol(objectName, instance.CreateNumericVector(source));
    }

    public static void CreateMatrix(double[,] source, string objectName)
    {
      var instance = REngine.GetInstance();
      instance.SetSymbol(objectName, instance.CreateNumericMatrix(source));
    }
  }
}
