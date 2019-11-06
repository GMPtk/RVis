using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using System;
using System.Linq;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace RVis.Base.Tests
{
  [TestClass()]
  public class UnitTestEnumExt
  {
    [Flags]
    private enum TestEnum
    {
      f1 = 0b0001,

      [Description("Test")]
      f2 = 0b0010,

      f3 = 0b0100
    }

    [TestMethod()]
    public void TestGetFlags()
    {
      // arrange
      var expected = new[] { 1, 2, 4 };

      // act
      var actual = EnumExt.GetFlags<TestEnum>().Cast<int>();

      // assert
      Assert.IsTrue(expected.SequenceEqual(actual));
    }

    [TestMethod()]
    public void TestIsAdd()
    {
      // arrange
      var subject1 = ObservableQualifier.Add | ObservableQualifier.Change | ObservableQualifier.Remove;
      var subject2 = ObservableQualifier.Change | ObservableQualifier.Remove;

      // act
      var isAdd1 = subject1.IsAdd();
      var isAdd2 = subject2.IsAdd();

      // assert
      Assert.IsTrue(isAdd1);
      Assert.IsFalse(isAdd2);
    }

    [TestMethod()]
    public void TestIsChange()
    {
      // arrange
      var subject1 = ObservableQualifier.Add | ObservableQualifier.Change | ObservableQualifier.Remove;
      var subject2 = ObservableQualifier.Add | ObservableQualifier.Remove;

      // act
      var isChange1 = subject1.IsChange();
      var isChange2 = subject2.IsChange();

      // assert
      Assert.IsTrue(isChange1);
      Assert.IsFalse(isChange2);
    }

    [TestMethod()]
    public void TestIsRemove()
    {
      // arrange
      var subject1 = ObservableQualifier.Add | ObservableQualifier.Change | ObservableQualifier.Remove;
      var subject2 = ObservableQualifier.Add | ObservableQualifier.Change;

      // act
      var isRemove1 = subject1.IsRemove();
      var isRemove2 = subject2.IsRemove();

      // assert
      Assert.IsTrue(isRemove1);
      Assert.IsFalse(isRemove2);
    }

    [TestMethod()]
    public void TestIsAddOrChange()
    {
      // arrange
      var subject1 = ObservableQualifier.Add | ObservableQualifier.Change | ObservableQualifier.Remove;
      var subject2 = ObservableQualifier.Remove;

      // act
      var is1 = subject1.IsAddOrChange();
      var is2 = subject2.IsAddOrChange();

      // assert
      Assert.IsTrue(is1);
      Assert.IsFalse(is2);
    }

    [TestMethod()]
    public void TestGetDescription()
    {
      // arrange
      var expected = "Test";

      // act
      var actual = TestEnum.f2.GetDescription();

      // assert
      Assert.AreEqual(expected, actual);
    }
  }
}