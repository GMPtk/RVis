using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Data;
using System;
using System.Linq;

namespace RVis.Client.Test
{
  [TestClass]
  public class UnitTestSource
  {
#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestEvaluate()
    {
      // arrange
      var invalidR = "seq({i * 10}, {i*30}, by={i*10})";
      var validR = "1:3";

      string invalidRMessage = default;
      NumDataColumn[] validRReturn;

      // act
      using (var server = new RVisServer())
      {
        using (var client = server.OpenChannel())
        {
          // invalid first to show channel is not left faulted
          try
          {
            client.EvaluateNumData(invalidR);
          }
          catch (Exception ex)
          {
            invalidRMessage = ex.Message;
          }

          validRReturn = client.EvaluateNumData(validR);
        }
      }

      // assert
      Assert.IsTrue(invalidRMessage.Contains("'i' not found"));
      Assert.IsTrue(new[] { 1.0, 2, 3 }.SequenceEqual(validRReturn[0].Data.ToArray()));
    }
#endif
  }
}
