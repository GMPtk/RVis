namespace Sensitivity
{
  internal struct RankedParameter
  {
    internal RankedParameter(string name, double score, bool isSelected)
    {
      Name = name;
      Score = score;
      IsSelected = isSelected;
    }

    internal string Name { get; }
    internal double Score { get; }
    internal bool IsSelected { get; }
  }
}
