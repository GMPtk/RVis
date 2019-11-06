using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using RVis.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace RVis.Model.Test
{
  [TestClass()]
  public class TestDistributionExt
  {
    [TestMethod()]
    public void TestToStringIfSome()
    {
      Assert.IsTrue(Some(Distribution.GetDefault(DistributionType.Normal)).ToStringIfSome("x").IsAString());
      Assert.IsNull(LangExt.NoneOf<IDistribution>().ToStringIfSome("x"));
    }

    [TestMethod()]
    public void TestSetDistribution()
    {
      // arrange
      var distributions = Distribution.GetDefaults();
      var toSet = new NormalDistribution(0d, 1d);
      
      // act
      distributions = distributions.SetDistribution(toSet);

      // assert
      Assert.IsTrue(distributions.Count(d => d.DistributionType == DistributionType.Normal) == 1);
      Assert.AreEqual(distributions.Single(d => d.DistributionType == DistributionType.Normal), toSet);
    }
  }
}