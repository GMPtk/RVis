using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestMeta
  {
    [TestMethod]
    public void TestProperties()
    {
      Assert.IsTrue(Meta.FileDate != default);
      Assert.IsTrue(Meta.Version != default);
      Assert.IsTrue(Meta.VersionMajorDotMinor != default);
      Assert.IsTrue(Meta.Product == nameof(RVis));
      Assert.IsTrue(Meta.Company == "HSE");
    }
  }
}
