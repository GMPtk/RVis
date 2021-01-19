using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LanguageExt.Prelude;

namespace RVisUI.Model.Test
{
  [TestClass()]
  public class UnitTestModuleInfo
  {
    [TestMethod()]
    public void TestConfigurationRoundTrip()
    {
      // arrange
      var moduleInfos = Range(1, 5, 2).Map(id => new ModuleInfo(
        $"{id:0000}",
        $"name{id:0000}",
        $"icon{id:0000}",
        $"desc{id:0000}",
        Array("pkgA", "pkgB"),
        default,
        Array("taskA", "taskB"),
        default!,
        $"{id:0000}.{id:0000}.{id:0000}.{id:0000}",
        $"key{id:0000}"
        )).ToArr();

      // act
      var configuration = ModuleInfo.GetModuleConfiguration(moduleInfos);
      var sortedAndEnabled = ModuleInfo.SortAndEnable(moduleInfos, configuration);

      var moduleInfosLessOne = moduleInfos.RemoveAt(1);
      var configurationLessOne = ModuleInfo.GetModuleConfiguration(moduleInfosLessOne);
      var sortedAndEnabledLessOne = ModuleInfo.SortAndEnable(moduleInfos, configurationLessOne);

      var moduleInfosReversed = moduleInfos.Reverse();
      var configurationReversed = ModuleInfo.GetModuleConfiguration(moduleInfosReversed);
      var sortedAndEnabledReversed = ModuleInfo.SortAndEnable(moduleInfos, configurationReversed);

      // assert
      Assert.AreEqual(moduleInfos, sortedAndEnabled);
      Assert.IsFalse(sortedAndEnabledLessOne[1].IsEnabled);
      Assert.AreEqual(moduleInfosReversed, sortedAndEnabledReversed);
    }
  }
}