using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LanguageExt.Prelude;

namespace RVis.Model.Test
{
  [TestClass]
  public class UnitTestSimObservationsSet
  {
    [TestMethod]
    public void TestAddRemoveObservations()
    {
      // arrange
      var simObservations = Range(1, 5, 2)
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
      var subject = new SimObservationsSet(simObservations.Head().Subject, simObservations);

      // act
      var (withAddition, added) = SimObservationsSet.AddObservations(subject, 100 + 2, $"refName{2:0000}", Array(7d, 8, 9), Array(10d, 11, 12));
      var withRemoval = SimObservationsSet.RemoveObservations(withAddition, added);
      var withAllRemoved = SimObservationsSet.RemoveAllObservations(withAddition, 100 + 2);

      // assert
      Assert.IsTrue(withAddition.Observations.Count == 6);
      Assert.IsTrue(added.ID == 10 + (5 * 2 - 1) + 1);
      Assert.IsTrue(withAddition.Observations.Exists(o => o.RefName == $"refName{2:0000}"));
      Assert.AreEqual(subject, withRemoval);
      Assert.AreEqual(subject, withAllRemoved);
      Assert.IsFalse(subject.Equals(withAddition));
    }
  }
}
