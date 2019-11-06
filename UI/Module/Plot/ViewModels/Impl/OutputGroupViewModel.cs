using static System.Globalization.CultureInfo;

namespace Plot
{
  public class OutputGroupViewModel : IOutputGroupViewModel
  {
    public OutputGroupViewModel(OutputGroup outputGroup)
    {
      OutputGroup = outputGroup;
      CreatedOn = OutputGroup.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss", InvariantCulture);
      Name = outputGroup.Name;
      Description = outputGroup.Description;
    }

    public OutputGroup OutputGroup { get; }

    public string CreatedOn { get; }

    public string Name { get; }

    public string Description { get; }
  }
}
