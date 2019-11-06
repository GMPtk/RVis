namespace RVis.Model
{
  internal class EvidenceSourceDTO
  {
    public int ID { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string[] Subjects { get; set; }

    public string RefName { get; set; }

    public string RefHash { get; set; }
  }
}
