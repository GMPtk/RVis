#nullable disable

namespace Plot.Design
{
  public class OutputGroupViewModel : IOutputGroupViewModel
  {
    public OutputGroup OutputGroup { get; set; }

    public string CreatedOn { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }
  }
}
