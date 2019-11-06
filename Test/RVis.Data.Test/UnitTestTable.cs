using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using System.IO;
using System.Linq;

namespace RVis.Data.Test
{
  [TestClass]
  public class UnitTestTable
  {
    [TestMethod]
    public void TestSerializeDataTable()
    {
      // arrange
      var columns = new[] {
        new NumDataColumn("doubles", new[] { 1.0, 2.0, 3.0 }),
        new NumDataColumn("more doubles", new[] { 4.0, 5.0, 6.0 })
      };
      var toSerialize = new NumDataTable("test", columns);

      // act
      var memoryStream = new MemoryStream();
      Serializer.Serialize(memoryStream, toSerialize);
      memoryStream.Position = 0;
      var deserialized = Serializer.Deserialize<NumDataTable>(memoryStream);

      // assert
      Assert.AreEqual(toSerialize.Name, deserialized.Name);
      Assert.IsTrue(toSerialize[0].Data.SequenceEqual(deserialized[0].Data));
      Assert.IsTrue(toSerialize[1].Data.SequenceEqual(deserialized[1].Data));
    }
  }
}
