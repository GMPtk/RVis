using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RVis.Model.Extensions;
using System.Linq;
using static LanguageExt.Prelude;

namespace RVis.Model.Test
{
  [TestClass()]
  public class UnitTestSimEvidenceExt
  {
    [TestMethod()]
    public void TestSimEvidenceSourceReferenceRoundTrip()
    {
      // arrange
      var evidenceSources = Range(1, 5, 2).Map(id =>
        new SimEvidenceSource(
          id,
          $"name{id:0000}",
          $"desc{id:0000}",
          toSet(Range(id, id).Map(s => $"subject{s:0000}")),
          $"refName{id:0000}",
          $"refHash{id:0000}"
          )
        )
        .ToArr();

      var mockEvidence = new Mock<ISimEvidence>();
      mockEvidence.Setup(me => me.EvidenceSources).Returns(evidenceSources);

      SimEvidenceSource expected = evidenceSources.Skip(1).Take(1).Single();
      ISimEvidence evidence = mockEvidence.Object;

      // act
      var reference = expected.GetReference();
      var actual = evidence.GetEvidenceSource(reference);

      // assert
      Assert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void TestSimObservationsReferenceRoundTrip()
    {
      // arrange
      var evidenceSources = Range(1, 5, 2).Map(id =>
        new SimEvidenceSource(
          100 + id,
          $"name{id:0000}",
          $"desc{id:0000}",
          toSet(Range(id, id).Map(s => $"subject{s:0000}")),
          $"refName{id:0000}",
          $"refHash{id:0000}"
          )
        )
        .ToArr();

      var observations = Range(1, 5, 2)
        .Map(
          id => new SimObservations(
            10 + id,
            100 + id,
            $"subjectX",
            $"refName{id:0000}",
            Array(1d, 2, 3),
            Array(4d, 5, 6)
            )
        )
      .ToArr();

      var observationsSet = new SimObservationsSet(observations.Head().Subject, observations);

      var mockEvidence = new Mock<ISimEvidence>();
      mockEvidence.Setup(me => me.EvidenceSources).Returns(evidenceSources);
      mockEvidence.Setup(me => me.GetObservationSet(It.Is<string>(s => s == "subjectX"))).Returns(observationsSet);
      ISimEvidence evidence = mockEvidence.Object;

      SimEvidenceSource evidenceSource = evidenceSources.Skip(1).Take(1).Single();
      SimObservations expected = observations.Skip(1).Take(1).Single();

      // act
      var reference = evidence.GetReference(expected);
      var actual = evidence.GetObservations(reference);

      // assert
      Assert.AreEqual(expected, actual);
    }
  }
}