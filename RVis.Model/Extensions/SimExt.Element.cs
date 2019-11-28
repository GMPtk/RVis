using LanguageExt;
using RVis.Base.Extensions;
using RVis.Data;
using System.Linq;
using static LanguageExt.Prelude;

namespace RVis.Model.Extensions
{
  public static partial class SimExt
  {
    public static NumDataColumn GetIndependentData(this SimOutput output, NumDataTable dataTable) =>
      dataTable.NumDataColumns.Single(ndc => ndc.Name == output.IndependentVariable.Name);

    public static Arr<NumDataColumn> GetDependentData(this SimOutput output, NumDataTable dataTable) =>
      output.DependentVariables.Map(e => dataTable[e.Name]);

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
