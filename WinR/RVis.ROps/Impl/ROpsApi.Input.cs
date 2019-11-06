using RDotNet;
using System;
using System.IO;
using System.Text;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    private static void SourceLines(string[] lines, REngine instance)
    {
      var source = String.Join("\n", lines);
      var bytes = Encoding.UTF8.GetBytes(source);
      var stream = new MemoryStream(bytes);
      instance.Evaluate(stream);
    }

    private static void SourceFile(string pathToCode, REngine instance)
    {
      var rPath = pathToCode.Replace("\\", "/");
      instance.Evaluate($"source(\"{rPath}\")");
    }
  }
}
