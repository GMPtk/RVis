using LanguageExt;
using OxyPlot;
using OxyPlot.Wpf;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace Sensitivity
{
  public sealed class OxyColorData
  {
    public static readonly Arr<OxyColorData> OxyColors = GetOxyColorDatas();

    public string Name { get; private set; }
    public Brush ColorBrush { get; private set; }
    public OxyColor OxyColor { get; private set; }

    private static Arr<OxyColorData> GetOxyColorDatas() =>
      typeof(OxyColors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Skip(1)
        .Select(cd => new { cd.Name, OxyColor = (OxyColor)cd.GetValue(null) })
        .Select(cd => new OxyColorData
        {
          Name = cd.Name,
          ColorBrush = ConverterExtensions.ToBrush(cd.OxyColor),
          OxyColor = cd.OxyColor
        })
        .ToArr();
  }
}
