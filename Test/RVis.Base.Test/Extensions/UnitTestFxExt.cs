using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Reflection;

namespace RVis.Base.Extensions.Tests
{
  [TestClass()]
  public class UnitTestFxExt
  {
    private static string NormalizePath(string path) =>
      Path.GetFullPath(new Uri(path).LocalPath)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .ToUpperInvariant();

    [TestMethod()]
    public void TestHasNoSchema()
    {
      // arrange
      var subject = new DataTable();

      // act
      var hasNoSchema1 = subject.HasNoSchema();
      subject.Columns.Add(new DataColumn());
      var hasNoSchema2 = subject.HasNoSchema();

      // assert
      Assert.IsTrue(hasNoSchema1);
      Assert.IsFalse(hasNoSchema2);
    }

    [TestMethod()]
    public void TestIsEmpty()
    {
      // arrange
      var subject = new DataTable();
      subject.Columns.Add(new DataColumn());

      // act
      var isEmpty1 = subject.IsEmpty();
      subject.Rows.Add(subject.NewRow());
      var isEmpty2 = subject.IsEmpty();

      // assert
      Assert.IsTrue(isEmpty1);
      Assert.IsFalse(isEmpty2);
    }

    [TestMethod()]
    public void TestGetDirectory()
    {
      // arrange
      var assembly = Assembly.GetExecutingAssembly();
      var fileName = Path.GetFileName(assembly.Location);

      // act
      var directory = assembly.GetDirectory();
      var codeBase = Path.Combine(directory, fileName);

      // assert
      Assert.AreEqual(NormalizePath(codeBase), NormalizePath(assembly.Location));
    }

    [TestMethod()]
    public void TestAsMmbrString()
    {
      // arrange
      var subject = new Version(101, 202, 303, 404);
      var expected = "101.202.00303.404";

      // act
      var actual = subject.AsMmbrString();

      // assert
      Assert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void TestIsGreaterThan()
    {
      Assert.IsTrue(2.IsGreaterThan(1));
      Assert.IsFalse(1.IsGreaterThan(2));
      Assert.IsFalse(1.IsGreaterThan(1));
    }

    [TestMethod()]
    public void TestIsGreaterThanOrEqualTo()
    {
      Assert.IsTrue(2.IsGreaterThanOrEqualTo(1));
      Assert.IsFalse(1.IsGreaterThanOrEqualTo(2));
      Assert.IsTrue(1.IsGreaterThanOrEqualTo(1));
    }

    [TestMethod()]
    public void TestIsLessThan()
    {
      Assert.IsTrue(1.IsLessThan(2));
      Assert.IsFalse(2.IsLessThan(1));
      Assert.IsFalse(1.IsLessThan(1));
    }

    [TestMethod()]
    public void TestIsLessThanOrEqualTo()
    {
      Assert.IsTrue(1.IsLessThanOrEqualTo(2));
      Assert.IsFalse(2.IsLessThanOrEqualTo(1));
      Assert.IsTrue(1.IsLessThanOrEqualTo(1));
    }

    [TestMethod()]
    public void TestInsertInOrdered()
    {
      // arrange
      var subject = new ObservableCollection<(int, int)>(new[] { (1, 111), (2, 222), (4, 444) });
      var item = (3, 333);
      var expected = 2;

      // act
      subject.InsertInOrdered(item, t => t.Item1);
      var actual = subject.IndexOf(item);

      // assert
      Assert.AreEqual(actual, expected);
    }
  }
}