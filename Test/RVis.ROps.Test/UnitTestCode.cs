using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Test;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RVis.ROps.Test
{
  [TestClass]
  public class UnitTestCode
  {
#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestSourceLines()
    {
      // arrange
      var lines = new[]
      {
        "a <- 1",
        "if(a == 1) {",
        "a <- 2",
        "}"
      };

      // act
      ROpsApi.SourceLines(lines);
      var @out = ROpsApi.EvaluateNumData("a");

      // assert
      Assert.AreEqual(@out.First().Data[0], 2.0);
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestEvaluate()
    {
      // arrange
      var expr01 = @"list(x = ""abc"", y = ""def"")";
      var expected01 = new Dictionary<string, object[]> { ["x"] = new[] { "abc" }, ["y"] = new[] { "def" } };

      // act
      var actual01 = ROpsApi.Evaluate(expr01);

      // assert
      Assert.AreEqual(expected01.Count, actual01.Count);

      var areEqual = expected01.Keys.All(
        k => actual01.ContainsKey(k) && actual01[k]?[0] as string == expected01[k][0] as string
        );
      Assert.IsTrue(areEqual);
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestEvaluateNumData()
    {
      // arrange
      var expr01 = "2+2";
      var expected01 = new[] { 4.0 };

      var expr02 = "list(x = 1:4, y = 5:8)";
      var x = Enumerable.Range(1, 4).Select(i => i * 1.0).ToArray();
      var y = Enumerable.Range(5, 4).Select(i => i * 1.0).ToArray();

      var expr03 = "seq(0,1,by=0.1)";
      var expected03 = Enumerable.Range(0, 11).Select(i => i * 0.1);

      // act
      var actual01 = ROpsApi.EvaluateNumData(expr01);
      var actual02 = ROpsApi.EvaluateNumData(expr02);
      var actual03 = ROpsApi.EvaluateNumData(expr03);

      // assert
      Assert.IsTrue(actual01.Length == 1);
      Assert.IsTrue(expected01.SequenceEqual(actual01[0].Data));

      Assert.IsTrue(actual02[0].Name == nameof(x) && x.SequenceEqual(actual02[0].Data));
      Assert.IsTrue(actual02[1].Name == nameof(y) && y.SequenceEqual(actual02[1].Data));

      Assert.IsTrue(expected03.SequenceEqual(actual03[0].Data));
    }
#endif
  }
}
