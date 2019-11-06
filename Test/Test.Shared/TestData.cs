using System.IO;

namespace RVis.Test
{
  internal static class TestData
  {
    internal static DirectoryInfo _simLibraryDirectory;
    internal static DirectoryInfo SimLibraryDirectory
    {
      get
      {
        if (null == _simLibraryDirectory)
        {
          var diTestBin = new DirectoryInfo(Directory.GetCurrentDirectory());
          var diTest = diTestBin.Parent;
          while (diTest.Name != nameof(Test)) diTest = diTest.Parent;
          var pathToSimLibrary = Path.Combine(diTest.FullName, "SimLibrary");
          _simLibraryDirectory = new DirectoryInfo(pathToSimLibrary);
        }
        return _simLibraryDirectory;
      }
    }
  }
}
