using LanguageExt;
using OxyPlot;
using OxyPlot.Wpf;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using static RVis.Base.Check;

namespace Sensitivity
{
  public sealed class OxyColorData
  {
    public static readonly Arr<OxyColorData> OxyColors = GetOxyColorDatas();

    public OxyColorData(string name, Brush colorBrush, OxyColor oxyColor)
    {
      Name = name;
      ColorBrush = colorBrush;
      OxyColor = oxyColor;
    }

    public string Name { get; }
    public Brush ColorBrush { get; }
    public OxyColor OxyColor { get; }

    private static Arr<OxyColorData> GetOxyColorDatas() =>
      typeof(OxyColors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Skip(1)
        .Select(cd => new { cd.Name, OxyColor = RequireInstanceOf<OxyColor>(cd.GetValue(null)) })
        .Select(cd => new OxyColorData(cd.Name, ConverterExtensions.ToBrush(cd.OxyColor), cd.OxyColor))
        .ToArr();
  }
}
