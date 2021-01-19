using System.IO;
using static RVis.Base.Check;

namespace RVis.Test
{
  internal static class TestData
  {
    internal static DirectoryInfo? _simLibraryDirectory;
    internal static DirectoryInfo SimLibraryDirectory
    {
      get
      {
        if (null == _simLibraryDirectory)
        {
          var diTestBin = new DirectoryInfo(Directory.GetCurrentDirectory());
          var diTest = diTestBin.Parent;
          RequireNotNull(diTest);
          while (diTest!.Name != nameof(Test)) diTest = diTest.Parent;
          var pathToSimLibrary = Path.Combine(diTest.FullName, "SimLibrary");
          _simLibraryDirectory = new DirectoryInfo(pathToSimLibrary);
        }
        return _simLibraryDirectory;
      }
    }
  }
}
