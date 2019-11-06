using LanguageExt;
using RVis.Base.Extensions;
using static LanguageExt.Prelude;

namespace RVis.Model.Extensions
{
  public static partial class SimExt
  {
    public static Option<SimElement> FindElement(this SimOutput output, string name)
    {
      foreach (var value in output.SimValues)
      {
        foreach (var element in value.SimElements)
        {
          if (element.Name == name) return Some(element);
        }
      }

      return None;
    }

    public static Arr<SimElement> FindElementsWithUnit(this SimOutput output, string unit) =>
      output.SimValues.Bind(v => v.SimElements.Filter(e => e.Unit == unit));

    public static string GetFQName(this SimElement element) =>
      element.Unit.IsAString() ? $"{element.Name} [{element.Unit}]" : element.Name;

    public static bool ContainsElement(this Arr<SimValue> values, string name) =>
      values.Exists(v => v.SimElements.Exists(e => e.Name == name));
  }
}
