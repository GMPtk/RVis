using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using static RVis.Base.Constant;

namespace Estimation.Test
{
  [TestClass]
  public class UnitTestCollExt
  {
    [TestMethod]
    public void TestSelectNearestY()
    {
      // arrange
      var sourceX = new[] { 1d, 3d, 5d };
      var sourceY = new[] { 7d, 8d, 9d };

      var targetX1 = Array.Empty<double>();
      var expectedY1 = Enumerable.Empty<double>();

      var targetX2 = new[] { 1d, 3d, 5d };
      var expectedY2 = new[] { 7d, 8d, 9d };

      var targetX3 = new[] { 0d, 10d };
      var expectedY3 = new[] { 7d, 9d };

      var targetX4 = new[] { 0.9d, 2.9d, 4.9d };
      var expectedY4 = new[] { 7d, 8d, 9d };

      var targetX5 = new[] { 1.1d, 3.1d, 5.1d };
      var expectedY5 = new[] { 7d, 8d, 9d };

      var targetX6 = new[] { 1.1d, 4d, 5d };
      var expectedY6 = new[] { 7d, 8d, 9d };

      var targetX7 = new[] { 1.1d, 4d + TOLERANCE, 5d };
      var expectedY7 = new[] { 7d, 8d, 9d };

      // act
      var actualY1 = targetX1.SelectNearestY(sourceX, sourceY);
      var actualY2 = targetX2.SelectNearestY(sourceX, sourceY);
      var actualY3 = targetX3.SelectNearestY(sourceX, sourceY);
      var actualY4 = targetX4.SelectNearestY(sourceX, sourceY);
      var actualY5 = targetX5.SelectNearestY(sourceX, sourceY);
      var actualY6 = targetX6.SelectNearestY(sourceX, sourceY);

      // assert
      Assert.IsTrue(expectedY1.SequenceEqual(actualY1));
      Assert.IsTrue(expectedY2.SequenceEqual(actualY2));
      Assert.IsTrue(expectedY3.SequenceEqual(actualY3));
      Assert.IsTrue(expectedY4.SequenceEqual(actualY4));
      Assert.IsTrue(expectedY5.SequenceEqual(actualY5));
      Assert.IsTrue(expectedY6.SequenceEqual(actualY6));
      Assert.ThrowsException<ArgumentException>(() => targetX7.SelectNearestY(sourceX, sourceY));
    }
  }
}
