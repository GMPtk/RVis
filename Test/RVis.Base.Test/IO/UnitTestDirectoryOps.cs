using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestDirectoryOps
  {
    [TestMethod]
    public void TestApplicationDataDirectory()
    {
      Assert.IsTrue(DirectoryOps.ApplicationDataDirectory.Exists);
    }

    [TestMethod]
    public void TestDocumentsDirectory()
    {
      Assert.IsTrue(DirectoryOps.DocumentsDirectory.Exists);
    }
  }
}
