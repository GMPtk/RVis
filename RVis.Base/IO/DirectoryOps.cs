using RVis.Base.Extensions;
using System.IO;
using static RVis.Base.Meta;
using static System.Environment;
using static System.IO.Directory;
using static System.IO.Path;
using static RVis.Base.Check;

namespace RVis.Base
{
  public static class DirectoryOps
  {
    public static DirectoryInfo ApplicationDataDirectory => GetApplicationDataDirectory(null);

    public static DirectoryInfo DocumentsDirectory => GetDocumentsDirectory(null);

    private static DirectoryInfo GetApplicationDataDirectory(string? subDirectory)
    {
      if (_applicationDataDirectory.IsntAString()) EnsureApplicationDataDirectory();

      RequireNotNullEmptyWhiteSpace(_applicationDataDirectory);

      if (subDirectory.IsntAString()) return new DirectoryInfo(_applicationDataDirectory);

      var path = Combine(_applicationDataDirectory, subDirectory);

      if (!Exists(path)) return CreateDirectory(path);

      return new DirectoryInfo(path);
    }

    private static DirectoryInfo GetDocumentsDirectory(string? subDirectory)
    {
      if (_documentsDirectory.IsntAString())
      {
        EnsureDocumentsDirectory();
      }

      RequireNotNullEmptyWhiteSpace(_documentsDirectory);

      if (subDirectory.IsntAString())
      {
        return new DirectoryInfo(_documentsDirectory);
      }

      var path = Combine(_documentsDirectory, subDirectory);
      if (!Exists(path))
      {
        return CreateDirectory(path);
      }

      return new DirectoryInfo(path);
    }

    private static void EnsureApplicationDataDirectory()
    {
      var path = GetFolderPath(SpecialFolder.LocalApplicationData);

      path = Combine(path, Company);

      if (!Exists(path)) CreateDirectory(path);

      path = Combine(path, Product);

      if (!Exists(path)) CreateDirectory(path);

      _applicationDataDirectory = path;
    }

    private static void EnsureDocumentsDirectory()
    {
      var path = GetFolderPath(SpecialFolder.MyDocuments);
      path = Combine(path, Product);
      if (!Exists(path)) CreateDirectory(path);
      _documentsDirectory = path;
    }

    private static string? _applicationDataDirectory;
    private static string? _documentsDirectory;
  }
}
