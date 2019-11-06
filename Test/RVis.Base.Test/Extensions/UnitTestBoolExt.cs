using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;

namespace RVis.Base.Test
{
  [TestClass()]
  public class UnitTestBoolExt
  {
    [TestMethod()]
    public void TestIsTrue()
    {
      // arrange
      bool? subject1 = true;
      bool? subject2 = false;
      bool? subject3 = default;

      // act
      var is1 = subject1.IsTrue();
      var is2 = subject2.IsTrue();
      var is3 = subject3.IsTrue();

      // assert
      Assert.IsTrue(is1);
      Assert.IsFalse(is2);
      Assert.IsFalse(is3);
    }
  }
}