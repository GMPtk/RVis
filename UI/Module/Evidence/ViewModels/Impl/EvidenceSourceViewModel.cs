namespace Evidence
{
  internal class EvidenceSourceViewModel : IEvidenceSourceViewModel
  {
    internal EvidenceSourceViewModel(int id, string name, string? description)
    {
      ID = id;
      Name = name;
      Description = description;
    }

    public int ID { get; }

    public string Name { get; }

    public string? Description { get; }
  }
}
