namespace RVis.Model
{
  public class TSimElement
  {
    public string Name { get; set; } = null!;

    public bool IsIndependentVariable { get; set; }

    public string? Unit { get; set; }

    public string? Description { get; set; }
  }
}
