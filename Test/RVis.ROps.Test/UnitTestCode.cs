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

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestInspectExecSymbols()
    {
      // arrange
      var memoryStream = new MemoryStream();
      var pathToSimLibrary = TestData.SimLibraryDirectory.FullName;
      var pathToCode = Path.Combine(pathToSimLibrary, "InspectExec", "inspect.R");
      const string execSymbol = "run";
      const string parametersSymbol = "parameters";
      const string p1Symbol = "p1";
      //const string p2Symbol = "p2";
      const string outputSymbol = "o";
      //const string o1Symbol = "o1";
      const string o2Symbol = "o2";
      const string o3Symbol = "o3";
      const string o3LocalSymbol = "Ro3";

      // act
      var symbolInfos = ROpsApi.InspectSymbols(pathToCode);
      var execSI = symbolInfos.SingleOrDefault(si => si.Symbol == execSymbol && si.Level == 0);
      var parameterSI = symbolInfos.SingleOrDefault(si => si.Symbol == parametersSymbol && si.Level == 0);
      var pDefs = parameterSI?.Names?.ToDictionary(n => n, n => symbolInfos.SingleOrDefault(si => si.Symbol == n && si.Level > 0));
      var outputSI = symbolInfos.SingleOrDefault(si => si.Symbol == outputSymbol && si.Level == 0);
      var oDefs = outputSI?.Names?.ToDictionary(n => n, n => symbolInfos.SingleOrDefault(si => si.Symbol == n && si.Level > 0));

      // assert
      Assert.IsNotNull(execSI);
      Assert.AreEqual("1st param", pDefs?[p1Symbol]?.Comment);
      Assert.AreEqual("u", pDefs?[p1Symbol]?.Unit);
      Assert.AreEqual("u.v", oDefs?[o2Symbol]?.Unit);
      Assert.AreEqual("2nd output", oDefs?[o2Symbol]?.Comment);
      Assert.IsFalse(oDefs?.ContainsKey(o3LocalSymbol) == true);
      Assert.IsNull(oDefs?[o3Symbol]);
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestInspectTmplSymbols()
    {
      // arrange
      var memoryStream = new MemoryStream();
      var pathToSimLibrary = TestData.SimLibraryDirectory.FullName;
      var pathToCode = Path.Combine(pathToSimLibrary, "InspectTmpl", "inspect.R");

      const string p1Symbol = "p1";
      const string p2Symbol = "p2";
      const string outputSymbol = "o";
      //const string o1Symbol = "o1";
      const string o2Symbol = "o2";
      const string o3Symbol = "o3";
      const string o3LocalSymbol = "Ro3";
      const string o4Symbol = "o4";

      // act
      var symbolInfos = ROpsApi.InspectSymbols(pathToCode);

      var p1SI = symbolInfos.SingleOrDefault(si => si.Symbol == p1Symbol && si.Level == 0);
      var p2SI = symbolInfos.SingleOrDefault(si => si.Symbol == p2Symbol && si.Level == 0);

      var outputSI = symbolInfos.SingleOrDefault(si => si.Symbol == outputSymbol && si.Level == 0);
      var oDefs = outputSI?.Names?.ToDictionary(n => n, n => symbolInfos.SingleOrDefault(si => si.Symbol == n && si.Level > 0));
      var o4SI = symbolInfos.SingleOrDefault(si => si.Symbol == o4Symbol && si.Level == 0);
      var o4LocalSI = symbolInfos.SingleOrDefault(si => si.Symbol == o4Symbol && si.Level > 0);

      // assert
      Assert.IsNotNull(p1SI);
      Assert.AreEqual("1st param", p1SI?.Comment);
      Assert.IsNotNull(p2SI);
      Assert.AreEqual("u", p1SI?.Unit);

      Assert.AreEqual("u.v", oDefs?[o2Symbol]?.Unit);
      Assert.AreEqual("2nd output", oDefs?[o2Symbol]?.Comment);
      Assert.IsFalse(oDefs?.ContainsKey(o3LocalSymbol) == true);
      Assert.IsNull(oDefs?[o3Symbol]);
      Assert.IsNotNull(o4SI);
      Assert.AreEqual("u/v", o4LocalSI?.Unit);
    }
#endif
  }
}
