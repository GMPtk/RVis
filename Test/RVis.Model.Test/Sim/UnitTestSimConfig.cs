using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nett;
using ProtoBuf;
using RVis.Test;
using System.IO;
using static LanguageExt.Prelude;
using static RVis.Model.Sim;

namespace RVis.Model.Test
{
  [TestClass]
  public class UnitTestSimConfig
  {
    [TestMethod]
    public void TestSerialize()
    {
      // arrange
      var memoryStream = new MemoryStream();
      var pathToSimLibrary = TestData.SimLibraryDirectory.FullName;
      var pathToConfig = Path.Combine(pathToSimLibrary, "CubicExec", ".rvis", "config.toml");
      var config = Sim.ReadConfigFromFile(pathToConfig);
      var expected = Toml.WriteString(config);

      // act
      Serializer.Serialize(memoryStream, FromToml(config));
      memoryStream.Position = 0;
      var deserialized = Serializer.Deserialize<SimConfig>(memoryStream);
      var actual = Toml.WriteString(ToToml(deserialized));

      // assert
      Assert.IsTrue(actual == expected);
    }
  }
}
