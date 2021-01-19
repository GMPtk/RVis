using LanguageExt;
using RVis.Data;
using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sampling
{
  internal sealed record FilterConfig
  {
    internal bool IsEnabled { get; init; }
    internal bool IsUnion { get; init; } = true;
    internal Arr<Filter> Filters { get; init; }

    internal static readonly FilterConfig Default = new FilterConfig 
    { 
      IsEnabled = false, 
      IsUnion = true, 
      Filters = default 
    };

    internal bool IsInFilteredSet(NumDataTable output)
    {
      var filters = Filters
        .Filter(fss => fss.IsEnabled)
        .Map(fss =>
        {
          var column = output[fss.OutputName];
          var datum = column[fss.At];
          return datum >= fss.From && datum <= fss.To;
        });

      if (filters.IsEmpty) return true;

      return IsUnion ? filters.Exists(f => f) : filters.ForAll(f => f);
    }
  }
}
