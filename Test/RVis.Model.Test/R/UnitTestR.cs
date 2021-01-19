using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using RVis.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RVis.Model.Test
{
  [TestClass]
  public class UnitTestR
  {
    [TestInitialize]
    public void TestInitialize()
    {
      serverPool = new RVisServerPool();
      serverPool.CreatePool(() => new RVisServer(), 2);
    }

    [TestCleanup]
    public void TestCleanup()
    {
      serverPool!.DestroyPool();
    }

    private RVisServerPool? serverPool;

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public async Task TestServerPoolRenewServerLicense()
    {
      // arrange
      var maybeServerLicense = serverPool!.RequestServer();
      var serverLicense = maybeServerLicense.AssertSome();
      var expected = DateTime.Now.Ticks.ToString("X");
      using (serverLicense)
      {
        var client = await serverLicense.GetRClientAsync();
        await client.ClearAsync();
        await client.EvaluateNonQueryAsync($"expected <- \"{expected}\"");
      }

      // act
      var maybeRenewedServerLicense = serverPool.RenewServerLicense(serverLicense);
      Assert.IsTrue(maybeRenewedServerLicense.IsSome);

      Dictionary<string, string[]> dictionary;
      using (var renewedServerLicense = maybeRenewedServerLicense.AssertSome())
      {
        var client = await renewedServerLicense.GetRClientAsync();
        dictionary = await client.EvaluateStringsAsync("expected");
      }

      // assert
      Assert.IsNotNull(dictionary);
      Assert.AreEqual(dictionary.Count, 1);

      var strings = dictionary.Values.First();
      Assert.AreEqual(strings?.Length, 1);

      var actual = strings?[0];
      Assert.AreEqual(actual, expected);
    }
#endif
  }
}
