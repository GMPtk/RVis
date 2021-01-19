using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using System.Linq;

namespace RVis.ROps.Test
{
  [TestClass]
  public class UnitTestPersistence
  {
#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestSerializeRoundTrip()
    {
      // arrange
      var lines = new[]
      {
        $"list_{nameof(TestSerializeRoundTrip)} <- list(ints = c(1,2,3), nums = c(1.0, 2.0, 3.), alpha = c(\"a\",\"b\",\"c\"))",
        $"copy_of_list_{nameof(TestSerializeRoundTrip)} <- list_{nameof(TestSerializeRoundTrip)}"
      };

      // act
      ROpsApi.SourceLines(lines);
      var bytes = ROpsApi.Serialize($"list_{nameof(TestSerializeRoundTrip)}");
      ROpsApi.EvaluateNonQuery($"rm(list_{nameof(TestSerializeRoundTrip)})");
      ROpsApi.Unserialize(bytes, $"list_{nameof(TestSerializeRoundTrip)}");
      var evaluated = ROpsApi.Evaluate($"all.equal(copy_of_list_{nameof(TestSerializeRoundTrip)}, list_{nameof(TestSerializeRoundTrip)})");

      // assert
      Assert.IsNotNull(evaluated.First().Value);
      Assert.IsTrue(evaluated.First().Value!.First().Resolve(out bool allEqual) && allEqual);
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestBinaryRoundTrip()
    {
      // arrange
      var lines = new[]
      {
        $"list_{nameof(TestBinaryRoundTrip)} <- list(ints = c(1,2,3), nums = c(1.0, 2.0, 3.), alpha = c(\"a\",\"b\",\"c\"))",
        $"copy_of_list_{nameof(TestBinaryRoundTrip)} <- list_{nameof(TestBinaryRoundTrip)}"
      };

      // act
      ROpsApi.SourceLines(lines);
      var bytes = ROpsApi.SaveObjectToBinary($"list_{nameof(TestBinaryRoundTrip)}");
      ROpsApi.EvaluateNonQuery($"rm(list_{nameof(TestBinaryRoundTrip)})");
      ROpsApi.LoadFromBinary(bytes);
      var evaluated = ROpsApi.Evaluate($"all.equal(copy_of_list_{nameof(TestBinaryRoundTrip)}, list_{nameof(TestBinaryRoundTrip)})");

      // assert
      Assert.IsNotNull(evaluated.First().Value);
      Assert.IsTrue(evaluated.First().Value!.First().Resolve(out bool allEqual) && allEqual);
    }
#endif
  }
}
