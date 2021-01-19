using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace RVis.Base.Test
{
  [TestClass()]
  public class UnitTestCollExt
  {
    [TestMethod()]
    public void TestFindIndex()
    {
      // arrange
      var subject = new[] { 1, 2, 3, 4, 4, 3, 2, 1 };
      var toFind1 = 4;
      var expected1 = 3;
      var toFind2 = 9;
      var expected2 = -1;

      // act
      var actual1 = subject.FindIndex(i => i == toFind1);
      var actual2 = subject.FindIndex(i => i == toFind2);

      // assert
      Assert.AreEqual(expected1, actual1);
      Assert.AreEqual(expected2, actual2);
      Assert.AreEqual(-1, Array.Empty<int>().FindIndex(i => i == default));
    }

    [TestMethod()]
    public void TestIsCollection()
    {
      // arrange
      var subject1 = new Collection<int>();
      var subject2 = new Collection<int>(new[] { 1, 2, 3 });
      Collection<int>? subject3 = default;

      // act
      var isColl1 = subject1.IsCollection();
      var isColl2 = subject2.IsCollection();
      var isColl3 = subject3.IsCollection();

      // assert
      Assert.IsFalse(isColl1);
      Assert.IsTrue(isColl2);
      Assert.IsFalse(isColl3);
    }

    [TestMethod()]
    public void TestIsEmpty()
    {
      // arrange
      var subject1 = new Collection<int>();
      var subject2 = new Collection<int>(new[] { 1, 2, 3 });

      // act
      var isEmpty1 = subject1.IsEmpty();
      var isEmpty2 = subject2.IsEmpty();

      // assert
      Assert.IsTrue(isEmpty1);
      Assert.IsFalse(isEmpty2);
    }

    [TestMethod()]
    public void TestRemoveIf()
    {
      // arrange
      var subject = new List<int>(new[] { 1, 2, 3, 4, 5 });
      var toRemove = new[] { 3, 4 };
      var expected = new[] { 1, 2, 5 };

      // act
      subject.RemoveIf(i => toRemove.Contains(i));

      // assert
      Assert.IsTrue(subject.SequenceEqual(expected));
    }

    [TestMethod()]
    public void TestMaxIndex()
    {
      // arrange
      var subject = new[] { 1, 2, 3, 4, 4, 3, 2, 1 };
      var expected = 3;

      // act
      var actual = subject.MaxIndex();

      // assert
      Assert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void TestMinIndex()
    {
      // arrange
      var subject = new[] { 1, 2, 3, 4, 4, 3, 2, 1 };
      var expected = 0;

      // act
      var actual = subject.MinIndex();

      // assert
      Assert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void TestAllSame()
    {
      // arrange
      var subject = new[] { (123,1), (123,2), (123,3) };

      // act
      var allSame = subject.AllSame(t => t.Item1);

      // assert
      Assert.IsTrue(allSame);
    }

    [TestMethod()]
    public void TestNotAllSame()
    {
      var subject = new[] { (123, 1), (123, 2), (321, 3) };

      // act
      var notAllSame = subject.NotAllSame(t => t.Item1);

      // assert
      Assert.IsTrue(notAllSame);
    }

    [TestMethod()]
    public void TestAllUnique()
    {
      // arrange
      var subject = new[] { (123, 1), (123, 2), (123, 3) };

      // act
      var allUnique = subject.AllUnique(t => t.Item2);

      // assert
      Assert.IsTrue(allUnique);
    }

    [TestMethod()]
    public void TestNotAllUnique()
    {
      // arrange
      var subject = new[] { (123, 1), (123, 2), (321, 3) };

      // act
      var notAllUnique = subject.NotAllUnique(t => t.Item1);

      // assert
      Assert.IsTrue(notAllUnique);
    }

    [TestMethod()]
    public void TestAdd()
    {
      // arrange
      var subject = new Dictionary<int, int> { (123, 1), (456, 2), (789, 3) };
      var toAdd = (0, 0);

      // act
      subject.Add(toAdd);

      // assert
      Assert.IsTrue(subject.Count == 4);
      Assert.IsTrue(subject[toAdd.Item1] == toAdd.Item2);
    }

    [TestMethod()]
    public void TestAddRange()
    {
      // arrange
      var subject = new Dictionary<int, int> { (123, 1), (456, 2), (789, 3) };
      var toAdd = new[] { (0, 0), (1, 1), (2, 2) };

      // act
      subject.AddRange(toAdd);

      // assert
      Assert.IsTrue(subject.Count == 6);
      Assert.IsTrue(toAdd.All(t => subject.ContainsKey(t.Item1)));
    }
  }
}