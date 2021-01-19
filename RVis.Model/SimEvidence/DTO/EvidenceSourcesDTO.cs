using LanguageExt;
using System.Linq;
using static LanguageExt.Prelude;

namespace RVis.Model
{
  internal class EvidenceSourcesDTO
  {
    internal static EvidenceSourcesDTO ToDTO(Arr<SimEvidenceSource> evidenceSources)
    {
      return new EvidenceSourcesDTO
      {
        EvidenceSources = evidenceSources
          .Map(es => new EvidenceSourceDTO
          {
            ID = es.ID,
            Name = es.Name,
            Description = es.Description,
            Subjects = es.Subjects.ToArray(),
            RefName = es.RefName,
            RefHash = es.RefHash
          })
          .ToArray()
      };
    }

    internal static Arr<SimEvidenceSource> FromDTO(EvidenceSourcesDTO evidenceSourcesDTO)
    {
      return evidenceSourcesDTO.EvidenceSources
        .Select(dto => new SimEvidenceSource(
          dto.ID, 
          dto.Name, 
          dto.Description, 
          toSet(dto.Subjects), 
          dto.RefName, 
          dto.RefHash
          ))
        .ToArr();
    }

    public EvidenceSourceDTO[] EvidenceSources { get; set; } = null!;
  }
}
