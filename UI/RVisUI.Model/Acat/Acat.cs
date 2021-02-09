using System.IO;
using System.Text;
using static RVis.Base.Check;
using static System.Environment;
using static System.IO.Path;

namespace RVisUI.Model
{
  public sealed class Acat
  {
    public static string Configure()
    {
      var pathToAcat = EnsureAssets();
      return pathToAcat;
    }

    private static string EnsureAssets()
    {
      var pathToUserProfile = GetFolderPath(SpecialFolder.UserProfile);
      var pathToAcat = Combine(pathToUserProfile, ".RVis", "ACAT");
      if (!Directory.Exists(pathToAcat))
      {
        Directory.CreateDirectory(pathToAcat);
      }

      var pathToModel = Combine(pathToAcat, "ACAT_like.model");
      if (!File.Exists(pathToModel))
      {
        using var stream = typeof(Acat).Assembly.GetManifestResourceStream("RVisUI.Model.Acat.ACAT_like.model");
        RequireNotNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var content = reader.ReadToEnd();
        File.WriteAllText(pathToModel, content);
      }

      var pathToReadMe = Combine(pathToAcat, "readme.txt");
      if (!File.Exists(pathToReadMe))
      {
        using var stream = typeof(Acat).Assembly.GetManifestResourceStream("RVisUI.Model.Acat.readme.txt");
        RequireNotNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var content = reader.ReadToEnd();
        File.WriteAllText(pathToReadMe, content);
      }

      return pathToAcat;
    }
  }
}
