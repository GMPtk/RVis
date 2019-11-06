using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestObjExt
  {
    [TestMethod]
    public void TestResolve()
    {
      Assert.IsFalse(new object().Resolve(out int _));
      Assert.IsTrue(((object)1).Resolve(out int _));
      Assert.IsFalse(((object)1d).Resolve(out int _));
    }
  }
}
