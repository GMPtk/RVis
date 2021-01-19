using System;

namespace RVis.Model
{
  public class TSimConfig
  {
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime ImportedOn { get; set; }

    public TSimCode Code { get; set; } = null!;

    public TSimInput Input { get; set; } = null!;

    public TSimOutput Output { get; set; } = null!;
  }
}
