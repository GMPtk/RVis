using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using System;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestTupleExt
  {
    [TestMethod]
    public void TestSnd()
    {
      Assert.AreEqual(new[] { (1, 111), (2, 222), (3, 333) }.Snd(2), 222);
      Assert.ThrowsException<InvalidOperationException>(() => new[] { (1, 111), (2, 222), (3, 333) }.Snd(4));
    }
  }
}
