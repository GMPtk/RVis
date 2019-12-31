using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using System;
using System.IO;
using System.Linq;
using static System.Environment;
using static System.Math;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestStrExt
  {
    [TestMethod]
    public void TestCheckParseValue()
    {
      Assert.AreEqual("123".CheckParseValue<int>(), "123");
      Assert.ThrowsException<ArgumentException>(() => "abc".CheckParseValue<int>());
    }

    [TestMethod]
    public void TestIsGreaterThan()
    {
      Assert.IsTrue("123".IsGreaterThan("0123"));
      Assert.IsFalse("0123".IsGreaterThan("123"));
      Assert.IsFalse("0123".IsGreaterThan("0123"));
    }

    [TestMethod]
    public void TestIsGreaterThanOrEqualTo()
    {
      Assert.IsTrue("123".IsGreaterThanOrEqualTo("0123"));
      Assert.IsFalse("0123".IsGreaterThanOrEqualTo("123"));
      Assert.IsTrue("0123".IsGreaterThanOrEqualTo("0123"));
    }

    [TestMethod]
    public void TestIsLessThan()
    {
      Assert.IsTrue("0123".IsLessThan("123"));
      Assert.IsFalse("123".IsLessThan("0123"));
      Assert.IsFalse("0123".IsLessThan("0123"));
    }

    [TestMethod]
    public void TestIsLessThanOrEqualTo()
    {
      Assert.IsTrue("0123".IsLessThanOrEqualTo("123"));
      Assert.IsFalse("123".IsLessThanOrEqualTo("0123"));
      Assert.IsTrue("0123".IsLessThanOrEqualTo("0123"));
    }

    [TestMethod]
    public void TestRejectEmpty()
    {
      Assert.AreEqual("abc".RejectEmpty(), "abc");
      Assert.IsTrue("   ".RejectEmpty() is null);
    }

    [TestMethod]
    public void TestToValidFileName()
    {
      Assert.AreEqual("ab.c".ToValidFileName(), "ab.c");
      Assert.AreEqual(@"a?*\/b.c".ToValidFileName(), "a_b.c");
    }

    [TestMethod]
    public void TestPascalToHyphenated()
    {
      Assert.AreEqual("onetwothree".PascalToHyphenated(), "onetwothree");
      Assert.AreEqual("OneTwoThree".PascalToHyphenated(), "One-Two-Three");
      Assert.AreEqual("One-Two-Three".PascalToHyphenated(), "One--Two--Three");
    }

    [TestMethod]
    public void TestToKey()
    {
      Assert.AreEqual("onetwothree".ToKey(), "onetwothree");
      Assert.AreEqual("one-two-three".ToKey(), "onetwothree");
    }

    [TestMethod]
    public void TestElide()
    {
      Assert.AreEqual("onetwothree".Elide(5), "on...");
      Assert.AreEqual("onetwothree".Elide(11), "onetwothree");
    }

    [TestMethod]
    public void TestExpandPath()
    {
      Assert.AreEqual(@"c:\one\two\three".ExpandPath(), @"c:\one\two\three");
      Assert.IsTrue(@"~/one\two\three".ExpandPath().EndsWith(@"one\two\three"));
      Assert.AreEqual(@"~/one\two\three".ExpandPath()[1], ':');
    }

    [TestMethod]
    public void TestContractPath()
    {
      Assert.AreEqual(@"c:\one\two\three".ContractPath(), @"c:\one\two\three");
      Assert.IsTrue(Path.Combine(GetFolderPath(SpecialFolder.MyDocuments), @"one\two\three").ContractPath().StartsWith("~/"));
    }

    [TestMethod]
    public void TestToHash()
    {
      Assert.AreEqual("12345678901234567890".ToHash(), "12345678901234567890".ToHash());
      Assert.AreNotEqual("12345678901234567890".ToHash(), "12345678901234567891".ToHash());
    }

    [TestMethod]
    public void TestReplace()
    {
      Assert.AreEqual("OneTwoThree".Replace('-', 'X'), "OneTwoThree");
      Assert.AreEqual("One-Two-Three".Replace('-', 'X'), "OneXTwoXThree");
      Assert.AreEqual("One--Two--Three".Replace('-', 'X'), "OneXXTwoXXThree");
    }

    [TestMethod]
    public void TestEqualsCI()
    {
      Assert.IsTrue("OneTwoThree".EqualsCI("onetwothree"));
      Assert.IsFalse("OneTwoThree".EqualsCI("onetwothreefour"));
    }

    [TestMethod]
    public void TestDoesNotEqualsCI()
    {
      Assert.IsFalse("OneTwoThree".DoesNotEqualCI("onetwothree"));
      Assert.IsTrue("OneTwoThree".DoesNotEqualCI("onetwothreefour"));
    }

    [TestMethod]
    public void TestIsAString()
    {
      Assert.IsFalse(default(string).IsAString());
      Assert.IsFalse("".IsAString());
      Assert.IsFalse(" ".IsAString());
      Assert.IsTrue("a".IsAString());
    }

    [TestMethod]
    public void TestIsntAString()
    {
      Assert.IsTrue(default(string).IsntAString());
      Assert.IsTrue("".IsntAString());
      Assert.IsTrue(" ".IsntAString());
      Assert.IsFalse("a".IsntAString());
    }

    [TestMethod]
    public void TestTokenize()
    {
      // arrange
      var toTokenize = "1.23,NA;;abc";
      var expected = new[] { "1.23", "NA", "abc" };

      // act
      var actual = toTokenize.Tokenize();

      // assert
      Assert.IsTrue(expected.SequenceEqual(actual));
    }

    [TestMethod]
    public void TestContainsWhiteSpace()
    {
      Assert.IsFalse(default(string).ContainsWhiteSpace());
      Assert.IsFalse("".ContainsWhiteSpace());
      Assert.IsTrue(" ".ContainsWhiteSpace());
      Assert.IsFalse("a".ContainsWhiteSpace());
    }

    [TestMethod]
    public void ToCsvQuoted()
    {
      Assert.AreEqual("a".ToCsvQuoted(), "a");
      Assert.AreEqual("a ".ToCsvQuoted(), "\"a \"");
      Assert.AreEqual(" a".ToCsvQuoted(), "\" a\"");
    }
  }
}
