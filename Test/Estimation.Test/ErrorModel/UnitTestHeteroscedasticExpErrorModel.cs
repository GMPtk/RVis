using Microsoft.VisualStudio.TestTools.UnitTesting;
using static RVis.Base.Check;
using static RVis.Base.Constant;

namespace Estimation.Test
{
  [TestClass]
  public class UnitTestHeteroscedasticExpErrorModel
  {
    [TestMethod]
    public void TestSerializationRoundTrip()
    {
      // arrange
      var expected = new HeteroscedasticExpErrorModel(1d, 0.1, 2d, 0.2, 3d);

      // act
      var serialized = expected.ToString();
      var deserialized = ErrorModel.DeserializeErrorModel(serialized);
      var errorModel = deserialized.IfNone(() => { Assert.Fail(); return default!; });

      var expectedPerturbed = RequireInstanceOf<HeteroscedasticExpErrorModel>(expected.GetPerturbed(default));
      var serializedPerturbed = expectedPerturbed.ToString();
      var deserializedPerturbed = ErrorModel.DeserializeErrorModel(serializedPerturbed);
      var errorModelPerturbed = deserializedPerturbed.IfNone(() => { Assert.Fail(); return default!; });

      // assert
      var actual = RequireInstanceOf<HeteroscedasticExpErrorModel>(errorModel);
      Assert.AreEqual(expected, actual);

      var actualPerturbed = RequireInstanceOf<HeteroscedasticExpErrorModel>(errorModelPerturbed);
      Assert.AreEqual(expectedPerturbed.Delta, actualPerturbed.Delta, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.DeltaStep, actualPerturbed.DeltaStep, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.DeltaStepInitializer, actualPerturbed.DeltaStepInitializer, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Sigma, actualPerturbed.Sigma, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.SigmaStep, actualPerturbed.SigmaStep, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.SigmaStepInitializer, actualPerturbed.SigmaStepInitializer, TOLERANCE);
      Assert.AreEqual(expectedPerturbed.Lower, actualPerturbed.Lower, TOLERANCE);
    }
  }
}
