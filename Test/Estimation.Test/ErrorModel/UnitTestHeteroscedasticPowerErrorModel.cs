using Microsoft.VisualStudio.TestTools.UnitTesting;
using static RVis.Base.Check;
using static RVis.Base.Constant;

namespace Estimation.Test
{
  [TestClass]
  public class UnitTestHeteroscedasticPowerErrorModel
  {
    [TestMethod]
    public void TestSerializationRoundTrip()
    {
      // arrange
      var expected = new HeteroscedasticPowerErrorModel(1d, 0.1, 2d, 0.2, 3d, 0.3, 4d);

      // act
      var serialized = expected.ToString();
      var deserialized = ErrorModel.DeserializeErrorModel(serialized);
      var errorModel = deserialized.IfNone(() => { Assert.Fail(); return default!; });

      var expectedPerturbed = RequireInstanceOf<HeteroscedasticPowerErrorModel>(expected.GetPerturbed(default));
      var serializedPerturbed = expectedPerturbed.ToString();
      var deserializedPerturbed = ErrorModel.DeserializeErrorModel(serializedPerturbed);
      var errorModelPerturbed = deserializedPerturbed.IfNone(() => { Assert.Fail(); return default!; });

      // assert
      var actual = RequireInstanceOf<HeteroscedasticPowerErrorModel>(errorModel);
      Assert.AreEqual(expected, actual);

      var actualPerturbed = RequireInstanceOf<HeteroscedasticPowerErrorModel>(errorModelPerturbed);
      Assert.AreEqual(expectedPerturbed.Delta1, actualPerturbed.Delta1, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Delta1Step, actualPerturbed.Delta1Step, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Delta1StepInitializer, actualPerturbed.Delta1StepInitializer, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Delta2, actualPerturbed.Delta2, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Delta2Step, actualPerturbed.Delta2Step, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Delta2StepInitializer, actualPerturbed.Delta2StepInitializer, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Sigma, actualPerturbed.Sigma, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.SigmaStep, actualPerturbed.SigmaStep, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.SigmaStepInitializer, actualPerturbed.SigmaStepInitializer, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Lower, actualPerturbed.Lower, TOLERANCE);
    }
  }
}
