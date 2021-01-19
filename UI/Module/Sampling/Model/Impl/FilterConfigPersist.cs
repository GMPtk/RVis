using RVis.Base.Extensions;
using System.Linq;

namespace Sampling
{
  internal class _FilterDTO
  {
    public string? OutputName { get; set; }
    public double From { get; set; }
    public double To { get; set; }
    public int At { get; set; }
    public bool IsEnabled { get; set; }
  }

  internal class _FilterConfigDTO
  {
    public bool IsEnabled { get; set; }
    public bool IsUnion { get; set; }
    public _FilterDTO[]? Filters { get; set; }
  }

  internal static class FilterConfigPersist
  {
    internal static FilterConfig FromDTO(this _FilterConfigDTO dto)
    {
      if (dto is null) return FilterConfig.Default;

      return new FilterConfig
      {

        IsEnabled = dto.IsEnabled,
        IsUnion = dto.IsUnion,
        Filters = dto.Filters?
          .Select(fs => new Filter(
            fs.OutputName.AssertNotNull(),
            fs.From,
            fs.To,
            fs.At,
            fs.IsEnabled
            ))
          .ToArr() ?? default
      };
    }

    internal static _FilterConfigDTO ToDTO(this FilterConfig filterConfig)
    {
      return new _FilterConfigDTO
      {
        IsEnabled = filterConfig.IsEnabled,
        IsUnion = filterConfig.IsUnion,
        Filters = filterConfig.Filters
          .Map(fs => new _FilterDTO
          {
            At = fs.At,
            From = fs.From,
            IsEnabled = fs.IsEnabled,
            OutputName = fs.OutputName,
            To = fs.To
          })
          .ToArray()
      };
    }
  }
}
