using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using System;

namespace RVis.Base.Test
{
  [TestClass()]
  public class UnitTestArrayExt
  {
    [TestMethod()]
    public void TestTranspose()
    {
      // arrange
      var subject1 = new int[][] { new[] { 1, 2 }, new[] { 3, 4 }, new[] { 5, 6 } };
      var subject2 = Array.Empty<int[]>();
      var expected1 = new int[][] { new[] { 1, 3, 5 }, new[] { 2, 4, 6 } };

      // act
      var actual1 = subject1.Transpose();
      var actual2 = subject2.Transpose();

      // assert
      Assert.IsTrue(expected1.Length == actual1.Length);
      Assert.IsTrue(expected1[0].Length == actual1[0].Length);

      for (int i = 0; i < expected1.Length; ++i)
        for (int j = 0; j < expected1[0].Length; ++j)
          Assert.AreEqual(expected1[i][j], actual1[i][j]);

      Assert.IsTrue(actual2.Length == 0);
    }

    [TestMethod()]
    public void TestToMultidimensional()
    {
      // arrange
      var subject1 = new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 }, new int[] { 5, 6 } };
      var expected1 = new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };

      // act
      var actual1 = subject1.ToMultidimensional();

      // assert
      Assert.IsTrue(expected1.GetLength(0) == actual1.GetLength(0));
      Assert.IsTrue(expected1.GetLength(1) == actual1.GetLength(1));

      for (int i = 0; i < expected1.GetLength(0); ++i)
        for (int j = 0; j < expected1.GetLength(1); ++j)
          Assert.AreEqual(expected1[i, j], actual1[i, j]);
    }

    [TestMethod()]
    public void TestToJagged()
    {
      // arrange
      var subject1 = new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
      var expected1 = new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 }, new int[] { 5, 6 } };

      // act
      var actual1 = subject1.ToJagged();

      // assert
      Assert.IsTrue(expected1.Length == actual1.Length);
      Assert.IsTrue(expected1[0].Length == actual1[0].Length);

      for (int i = 0; i < expected1.Length; ++i)
        for (int j = 0; j < expected1[0].Length; ++j)
          Assert.AreEqual(expected1[i][j], actual1[i][j]);
    }

    [TestMethod()]
    public void TestIsNullOrEmpty()
    {
      // arrange
      object[]? subject1 = default;
      object[] subject2 = Array.Empty<object>();

      // act
      var isNull = subject1.IsNullOrEmpty();
      var isEmpty = subject2.IsNullOrEmpty();

      // assert
      Assert.IsTrue(isNull);
      Assert.IsTrue(isEmpty);
    }

    [TestMethod()]
    public void TestIsEmpty()
    {
      // arrange
      object[] subject = Array.Empty<object>();

      // act
      var isEmpty = subject.IsEmpty();

      // assert
      Assert.IsTrue(isEmpty);
    }
  }
}