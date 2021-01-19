using Microsoft.VisualStudio.TestTools.UnitTesting;
using static RVis.Base.Check;
using static RVis.Base.Constant;

namespace Estimation.Test
{
  [TestClass]
  public class UnitTestNormalErrorModel
  {
    [TestMethod]
    public void TestSerializationRoundTrip()
    {
      // arrange
      var expected = new NormalErrorModel(1d, 0.1);

      // act
      var serialized = expected.ToString();
      var deserialized = ErrorModel.DeserializeErrorModel(serialized);
      var errorModel = deserialized.IfNone(() => { Assert.Fail(); return default!; });

      var expectedPerturbed = RequireInstanceOf<NormalErrorModel>(expected.GetPerturbed(default));
      var serializedPerturbed = expectedPerturbed.ToString();
      var deserializedPerturbed = ErrorModel.DeserializeErrorModel(serializedPerturbed);
      var errorModelPerturbed = deserializedPerturbed.IfNone(() => { Assert.Fail(); return default!; });

      // assert
      var actual = RequireInstanceOf<NormalErrorModel>(errorModel);
      Assert.AreEqual(expected, actual);

      var actualPerturbed = RequireInstanceOf<NormalErrorModel>(errorModelPerturbed);
      Assert.AreEqual(expectedPerturbed.Sigma, actualPerturbed.Sigma, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Step, actualPerturbed.Step, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.StepInitializer, actualPerturbed.StepInitializer, TOLERANCE);
    }
  }
}
