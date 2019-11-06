using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using static System.Double;
using static System.Math;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestNumExt
  {
    [TestMethod]
    public void TestToNullable()
    {
      Assert.IsTrue(NaN.ToNullable() is null);
      Assert.IsTrue(1d.ToNullable().HasValue);
    }

    [TestMethod]
    public void TestFromNullable()
    {
      Assert.IsTrue(IsNaN(((double?)NaN).FromNullable()));
      Assert.IsTrue(IsNaN(default(double?).FromNullable()));
      Assert.AreEqual(((double?)1d).FromNullable(), 1d);
    }

    [TestMethod]
    public void TestGetSignum()
    {
      Assert.AreEqual(123d.GetSignum(), 1d);
      Assert.AreEqual(0d.GetSignum(), 0d);
      Assert.AreEqual(-123d.GetSignum(), -1d);
    }

    [TestMethod]
    public void TestIsFound()
    {
      Assert.IsTrue(0.IsFound());
      Assert.IsFalse((-1).IsFound());
    }

    [TestMethod]
    public void TestIsntFound()
    {
      Assert.IsTrue((-1).IsntFound());
      Assert.IsFalse(0.IsntFound());
    }

    [TestMethod]
    public void TestToSigFigs()
    {
      // arrange
      var subject = 123.456;
      var expected = 1.2E2;

      // act
      var actual = subject.ToSigFigs(2);

      // assert
      Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TestIsEqualTo()
    {
      // arrange
      var expected = 2.0;

      // act
      var sqrtTwo = Sqrt(expected);
      var actual = sqrtTwo * sqrtTwo;

      // assert
      Assert.IsTrue(actual.IsEqualTo(expected));
    }

    [TestMethod]
    public void TestIsInClosedInterval()
    {
      Assert.IsTrue(5d.IsInClosedInterval(4d, 6d));
      Assert.IsTrue(5d.IsInClosedInterval(5d, 6d));
      Assert.IsTrue(5d.IsInClosedInterval(4d, 5d));
      Assert.IsFalse(5d.IsInClosedInterval(5d + Constant.TOLERANCE, 6d));
    }

    [TestMethod]
    public void TestGetPreviousOrderOfMagnitude()
    {
      Assert.AreEqual(100d, 123d.GetPreviousOrderOfMagnitude());
      Assert.AreEqual(-100d, -123d.GetPreviousOrderOfMagnitude());
    }

    [TestMethod]
    public void TestGetNextOrderOfMagnitude()
    {
      Assert.AreEqual(1000d, 123d.GetNextOrderOfMagnitude());
      Assert.AreEqual(-1000d, -123d.GetNextOrderOfMagnitude());
    }
  }
}
