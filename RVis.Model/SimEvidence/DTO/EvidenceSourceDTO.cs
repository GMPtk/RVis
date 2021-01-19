namespace RVis.Model
{
  internal class EvidenceSourceDTO
  {
    public int ID { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string[] Subjects { get; set; } = null!;

    public string RefName { get; set; } = null!;

    public string RefHash { get; set; } = null!;
  }
}
