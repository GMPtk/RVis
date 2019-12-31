using LanguageExt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using static LanguageExt.Prelude;

namespace Sampling.Test
{
  internal static class Compare
  {
    internal static bool MatrixEquals<T>(this T[,] lhs, T[,] rhs) where T : IComparable
    {
      if (lhs.GetLength(0) != rhs.GetLength(0) || lhs.GetLength(1) != rhs.GetLength(1))
      {
        return false;
      }

      var comparer = EqualityComparer<T>.Default;

      for (int i = 0; i < lhs.GetLength(0); ++i)
        for (int j = 0; j < lhs.GetLength(1); ++j)
          if (!comparer.Equals(lhs[i, j], rhs[i, j]))
            return false;
      
      return true;
    }
  }

  [TestClass]
  public class UnitTestModelExt
  {
    [TestMethod]
    public void TestCorrelationsToMatrix()
    {
      // arrange
      var correlations1 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var expected1 = new double[,]
      {
        {1, 3, 2, 1},
        {3, 1, 5, 4},
        {2, 5, 1, 6},
        {1, 4, 6, 1}
      };

      var correlations2 = Array(
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var expected2 = new double[,]
      {
        {1, 6},
        {6, 1}
      };

      // act
      var actual1 = correlations1.CorrelationsToMatrix();
      var actual2 = correlations2.CorrelationsToMatrix();

      // assert
      Assert.IsTrue(expected1.MatrixEquals(actual1));
      Assert.IsTrue(expected2.MatrixEquals(actual2));
    }

    [TestMethod]
    public void TestIsValidCorrelations()
    {
      // arrange
      var valid1 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );
      var valid2 = Array(("param01", Arr<double>.Empty));

      var invalid1 = Array(
        ("param02", Array(3d, 2, 1)),
        ("param01", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );
      var invalid2 = Array(
        ("param01", Array(2d, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );
      var invalid3 = Array(
        ("param01", Array(2d, 1))
        );

      // act
      var valid1IsValid = valid1.IsValidCorrelations();
      var valid2IsValid = valid2.IsValidCorrelations();
      var invalid1IsInvalid = !invalid1.IsValidCorrelations();
      var invalid2IsInvalid = !invalid2.IsValidCorrelations();
      var invalid3IsInvalid = !invalid3.IsValidCorrelations();

      // assert
      Assert.IsTrue(valid1IsValid);
      Assert.IsTrue(valid2IsValid);
      Assert.IsTrue(invalid1IsInvalid);
      Assert.IsTrue(invalid2IsInvalid);
      Assert.IsTrue(invalid3IsInvalid);
    }

    [TestMethod]
    public void TestUpdateCorrelations()
    {
      // arrange
      var correlations1 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var update1 = Array(
        ("param01", Array(30d, 20, 10)),
        ("param02", Array(50d, 40)),
        ("param03", Array(60d)),
        ("param04", Arr<double>.Empty)
        );

      var expected1 = Array(
        ("param01", Array(30d, 20, 10)),
        ("param02", Array(50d, 40)),
        ("param03", Array(60d)),
        ("param04", Arr<double>.Empty)
        );

      var correlations2 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var update2 = Array(
        ("param02", Array(7d)),
        ("param04", Arr<double>.Empty)
        );

      var expected2 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 7)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var correlations3 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var update3 = Array(
        ("param01", Array(7d)),
        ("param02", Arr<double>.Empty)
        );

      var expected3 = Array(
        ("param01", Array(7d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var correlations4 = Array(
        ("param01", Array(1d)),
        ("param04", Arr<double>.Empty)
        );

      var update4 = Array(
        ("param02", Array(2d)),
        ("param03", Arr<double>.Empty)
        );

      var expected4 = Array(
        ("param01", Array(0d, 0, 1)),
        ("param02", Array(2d, 0)),
        ("param03", Array(0d)),
        ("param04", Arr<double>.Empty)
        );

      var correlations5 = Array(
        ("param01", Array(1d)),
        ("param02", Arr<double>.Empty)
        );

      var update5 = Array(
        ("param03", Array(2d)),
        ("param04", Arr<double>.Empty)
        );

      var expected5 = Array(
        ("param01", Array(1d, 0, 0)),
        ("param02", Array(0d, 0)),
        ("param03", Array(2d)),
        ("param04", Arr<double>.Empty)
        );

      // act
      var actual1 = correlations1.UpdateCorrelations(update1);
      var actual2 = correlations2.UpdateCorrelations(update2);
      var actual3 = correlations3.UpdateCorrelations(update3);
      var actual4 = correlations4.UpdateCorrelations(update4);
      var actual5 = correlations5.UpdateCorrelations(update5);

      // assert
      Assert.AreEqual(expected1, actual1);
      Assert.AreEqual(expected2, actual2);
      Assert.AreEqual(expected3, actual3);
      Assert.AreEqual(expected4, actual4);
      Assert.AreEqual(expected5, actual5);
    }

    [TestMethod]
    public void TestCorrelationsFor()
    {
      // arrange
      var correlations1 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var parameters1 = Array(
        "param01",
        "param02",
        "param03",
        "param04"
        );

      var expected1 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var correlations2 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var parameters2 = Array(
        "param02",
        "param04"
        );

      var expected2 = Array(
        ("param02", Array(4d)),
        ("param04", Arr<double>.Empty)
        );

      var correlations3 = Array(
        ("param01", Array(3d, 2, 1)),
        ("param02", Array(5d, 4)),
        ("param03", Array(6d)),
        ("param04", Arr<double>.Empty)
        );

      var parameters3 = Array(
        "param01",
        "param02"
        );

      var expected3 = Array(
        ("param01", Array(3d)),
        ("param02", Arr<double>.Empty)
        );

      var correlations4 = Array(
        ("param01", Array(1d)),
        ("param04", Arr<double>.Empty)
        );

      var parameters4 = Array(
        "param02",
        "param03"
        );

      var expected4 = Array(
        ("param02", Array(0d)),
        ("param03", Arr<double>.Empty)
        );

      // act
      var actual1 = correlations1.CorrelationsFor(parameters1);
      var actual2 = correlations2.CorrelationsFor(parameters2);
      var actual3 = correlations3.CorrelationsFor(parameters3);
      var actual4 = correlations4.CorrelationsFor(parameters4);

      // assert
      Assert.AreEqual(expected1, actual1);
      Assert.AreEqual(expected2, actual2);
      Assert.AreEqual(expected3, actual3);
      Assert.AreEqual(expected4, actual4);
    }

    [TestMethod]
    public void TestAddCorrelation()
    {
      // arrange
      var correlations = Array(
        ("param02", Array(3d, 2, 1)),
        ("param04", Array(5d, 4)),
        ("param06", Array(6d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation1 = "param01";

      var expected1 = Array(
        ("param01", Array(0d, 0, 0, 0)),
        ("param02", Array(3d, 2, 1)),
        ("param04", Array(5d, 4)),
        ("param06", Array(6d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation2 = "param03";

      var expected2 = Array(
        ("param02", Array(0, 3d, 2, 1)),
        ("param03", Array(0d, 0, 0)),
        ("param04", Array(5d, 4)),
        ("param06", Array(6d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation3 = "param05";

      var expected3 = Array(
        ("param02", Array(3d, 0, 2, 1)),
        ("param04", Array(0, 5d, 4)),
        ("param05", Array(0d, 0)),
        ("param06", Array(6d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation4 = "param07";

      var expected4 = Array(
        ("param02", Array(3d, 2, 0d, 1)),
        ("param04", Array(5d, 0d, 4)),
        ("param06", Array(0, 6d)),
        ("param07", Array(0d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation5 = "param09";

      var expected5 = Array(
        ("param02", Array(3d, 2, 1, 0)),
        ("param04", Array(5d, 4, 0)),
        ("param06", Array(6d, 0)),
        ("param08", Array(0d)),
        ("param09", Arr<double>.Empty)
        );

      // act
      var actual1 = correlations.AddCorrelation(correlation1);
      var actual2 = correlations.AddCorrelation(correlation2);
      var actual3 = correlations.AddCorrelation(correlation3);
      var actual4 = correlations.AddCorrelation(correlation4);
      var actual5 = correlations.AddCorrelation(correlation5);

      // assert
      Assert.AreEqual(expected1, actual1);
      Assert.AreEqual(expected2, actual2);
      Assert.AreEqual(expected3, actual3);
      Assert.AreEqual(expected4, actual4);
      Assert.AreEqual(expected5, actual5);
    }

    [TestMethod]
    public void TestRemoveCorrelation()
    {
      // arrange
      var correlations = Array(
        ("param02", Array(3d, 2, 1)),
        ("param04", Array(5d, 4)),
        ("param06", Array(6d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation1 = "param02";

      var expected1 = Array(
        ("param04", Array(5d, 4)),
        ("param06", Array(6d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation2 = "param04";

      var expected2 = Array(
        ("param02", Array(2d, 1)),
        ("param06", Array(6d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation3 = "param06";

      var expected3 = Array(
        ("param02", Array(3d, 1)),
        ("param04", Array(4d)),
        ("param08", Arr<double>.Empty)
        );

      var correlation4 = "param08";

      var expected4 = Array(
        ("param02", Array(3d, 2)),
        ("param04", Array(5d)),
        ("param06", Arr<double>.Empty)
        );

      // act
      var actual1 = correlations.RemoveCorrelation(correlation1);
      var actual2 = correlations.RemoveCorrelation(correlation2);
      var actual3 = correlations.RemoveCorrelation(correlation3);
      var actual4 = correlations.RemoveCorrelation(correlation4);

      // assert
      Assert.AreEqual(expected1, actual1);
      Assert.AreEqual(expected2, actual2);
      Assert.AreEqual(expected3, actual3);
      Assert.AreEqual(expected4, actual4);
    }
  }
}
