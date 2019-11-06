using System;

namespace RVis.Model
{
  public class TSimConfig
  {
    public string Title { get; set; }

    public string Description { get; set; }

    public DateTime ImportedOn { get; set; }

    public TSimCode Code { get; set; }

    public TSimInput Input { get; set; }

    public TSimOutput Output { get; set; }
  }
}
