using LanguageExt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using System;
using static LanguageExt.Prelude;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestLangExt
  {
    [TestMethod]
    public void TestAssertSome()
    {
      Assert.IsTrue(Some(true).AssertSome());
      Assert.ThrowsException<ValueIsNoneException>(() => Option<bool>.None.AssertSome());
    }

    [TestMethod]
    public void TestAssertRight()
    {
      Assert.IsTrue(toEither<Exception?, bool>(true, default(Exception)).AssertRight());
      Assert.ThrowsException<EitherIsNotRightException>(() => toEither<Exception, bool>(default, () => new Exception()).AssertRight());
      Assert.ThrowsException<BottomException>(() => Either<Exception, bool>.Bottom.AssertRight());
    }

    [TestMethod]
    public void TestLookUp()
    {
      // arrange
      var subject = Array((1, 2), (3, 4), (5, 6));
      var expected1 = Some(4);
      var expected2 = None;

      // act
      var actual1 = subject.LookUp(3);
      var actual2 = subject.LookUp(123);

      // assert
      Assert.AreEqual(expected1, actual1);
      Assert.AreEqual(expected2, actual2);
    }

    [TestMethod]
    public void TestContainsNone()
    {
      Assert.IsTrue(new[] { Some(1), None }.ContainsNone());
      Assert.IsFalse(new[] { Some(1), Some(2) }.ContainsNone());
      Assert.IsFalse(System.Array.Empty<Option<int>>().ContainsNone());
    }
  }
}
