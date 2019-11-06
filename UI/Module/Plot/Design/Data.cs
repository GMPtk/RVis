using RVis.Data;
using RVis.Data.Extensions;
using System.Linq;

namespace Plot.Design
{
  internal static class Data
  {
    static Data()
    {
      var independent = Enumerable.Range(0, 9).Select(i => i * 1000.0).ToDataColumn("RPM");
      var hpPerRpm = new[] { 0.0, 24, 52, 74, 98, 112, 124, 122, 116 }.ToDataColumn("HP per RPM");
      var torquePerRpm = new[] { 0.0, 22, 45, 54, 58, 55, 50, 47, 45 }.ToDataColumn("Torque per RPM");
      HPTorqueOverRPM = new NumDataTable("HP/Torque by RPM", independent, hpPerRpm, torquePerRpm);
    }

    internal static NumDataTable HPTorqueOverRPM { get; }

    internal const string LorumIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
  }
}
