using RVis.Base.Extensions;
using System;
using System.IO;
using System.Reflection;
using static RVis.Base.Check;

namespace RVis.Base
{
  public static class Meta
  {
    public static DateTime FileDate
    {
      get
      {
        var assembly = Assembly.GetExecutingAssembly();
        return File.GetLastWriteTime(assembly.Location);
      }
    }

    public static string Version
    {
      get
      {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        RequireNotNull(version);
        return version.AsMmbrString();
      }
    }

    public static string VersionMajorDotMinor
    {
      get
      {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        RequireNotNull(version);
        return $"{version.Major}.{version.Minor}";
      }
    }

    public static string Product =>
      ((AssemblyProductAttribute)Attribute.GetCustomAttribute(
        Assembly.GetExecutingAssembly(),
        typeof(AssemblyProductAttribute)
        )!
      ).Product;

    public static string Company =>
      ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
        Assembly.GetExecutingAssembly(),
        typeof(AssemblyCompanyAttribute)
        )!
      ).Company;
  }
}
