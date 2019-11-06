using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LanguageExt.Prelude;

namespace RVis.Model.Test
{
  [TestClass]
  public class UnitTestSimInput
  {
    [TestMethod]
    public void TestEquality()
    {
      // arrange
      var input1 = new SimInput(Array(
        new SimParameter("n1", "v1", default, default),
        new SimParameter("n2", "v2", default, default)
        ), isDefault: false);

      var input2 = new SimInput(Array(
        new SimParameter("n1", "v1", default, default),
        new SimParameter("n2", "v2", default, default)
        ), isDefault: false);

      // act
      var areEqual = input1 == input2;

      // assert
      Assert.IsTrue(areEqual);
    }
  }
}
