using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using System;

namespace RVis.Base.Test
{
  [TestClass()]
  public class UnitTestArrayExt
  {
    [TestMethod()]
    public void TestIsNullOrEmpty()
    {
      // arrange
      object[] subject1 = default;
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