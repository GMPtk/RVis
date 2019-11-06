using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestCheck
  {
    [TestMethod]
    public void TestRequireInstanceOf()
    {
      Assert.ThrowsException<ArgumentException>(() => Check.RequireInstanceOf<int>((object)"123"));
    }

    [TestMethod]
    public void TestRequireUniqueElements()
    {
      Assert.ThrowsException<ArgumentException>(() => Check.RequireUniqueElements(new[] {(1,111), (2,222), (1, 333) }, t => t.Item1));
    }
  }
}
