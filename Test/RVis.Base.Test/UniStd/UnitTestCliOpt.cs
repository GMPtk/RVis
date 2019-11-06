using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using static LanguageExt.Prelude;

namespace RVis.Base.Test
{
  [TestClass]
  public class UnitTestCliOpt
  {
    private enum TestOption
    {
      None,
      OptionWithShort, o,
      OptionWithoutShort,
      OptionArgWithShort, a,
      OptionArgWithoutShort
    }

    [TestMethod]
    public void TestToCliOptSpecs()
    {
      var cliOptSpecs = Seq(

        $"{TestOption.OptionWithShort}", $"{TestOption.o}",
        $"{TestOption.OptionWithoutShort}",

        $"{TestOption.OptionArgWithShort}=", $"{TestOption.a}=",
        $"{TestOption.OptionArgWithoutShort}="

      ).ToCliOptSpecs();

      Assert.IsTrue(cliOptSpecs.Count == 6);
      Assert.AreEqual(cliOptSpecs[0].Option, "option-with-short");
      Assert.IsTrue(cliOptSpecs[0].OptArgType == OptArgType.LongOpt);
      Assert.IsTrue(cliOptSpecs[0].OptArgUsage == OptArgUsage.None);
    }

    [TestMethod]
    public void TestToCliOpts()
    {
      var cliOptSpecs = Seq(

        $"{TestOption.OptionWithShort}", $"{TestOption.o}",
        $"{TestOption.OptionWithoutShort}",

        $"{TestOption.OptionArgWithShort}=", $"{TestOption.a}=",
        $"{TestOption.OptionArgWithoutShort}="

      ).ToCliOptSpecs();

      var args = new[] { $"--option-arg-without-short=arg" };

      var cliOpts = args.ToCliOpts(cliOptSpecs);

      var option = cliOpts.GetOpt(TestOption.OptionArgWithoutShort);

      Assert.AreEqual(option.AssertSome().Argument.AssertSome(), "arg");
    }
  }
}
