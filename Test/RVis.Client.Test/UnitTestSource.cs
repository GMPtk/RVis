using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using RVis.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RVis.Client.Test
{
  [TestClass]
  public class UnitTestSource
  {
#if !IS_PIPELINES_BUILD
    [TestMethod]
    public async Task TestEvaluate()
    {
      // arrange
      var invalidR = "seq({i * 10}, {i*30}, by={i*10})";
      var validR = "1:3";

      string? invalidRMessage = default;
      NumDataColumn[] validRReturn;

      // act
      using (var server = new RVisServer())
      {
        var client = await server.OpenChannelAsync();
        
        // invalid first to show channel is not left faulted
        try
        {
          await client.EvaluateNumDataAsync(invalidR);
        }
        catch (Exception ex)
        {
          invalidRMessage = ex.Message;
        }

        validRReturn = await client.EvaluateNumDataAsync(validR);
      }

      // assert
      Assert.IsTrue(invalidRMessage?.Contains("'i' not found") == true);
      Assert.IsTrue(new[] { 1.0, 2, 3 }.SequenceEqual(validRReturn[0].Data.ToArray()));
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public async Task TestCreateVector()
    {
      // arrange
      var source01 = Array.Empty<double>();
      var source02 = new[] { 1d, 2, 3 };

      var expected01 = source01;
      var expected02 = source02;

      double[]? actual01;
      double[]? actual02;

      // act
      using (var server = new RVisServer())
      {
        var client = await server.OpenChannelAsync();

        await client.CreateVectorAsync(source01, nameof(source01));
        await client.CreateVectorAsync(source02, nameof(source02));

        var doubles = await client.EvaluateDoublesAsync(nameof(source01));
        actual01 = doubles.Single().Value?.ToArray();

        doubles = await client.EvaluateDoublesAsync(nameof(source02));
        actual02 = doubles.Single().Value?.ToArray();
      }

      // assert
      Assert.AreEqual(expected01, actual01);
      Assert.IsTrue(expected02.SequenceEqual(actual02!));
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public async Task TestCreateMatrix()
    {
      // arrange
      var source01 = new double[0, 0] { };
      var source02 = new double[3, 3] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };

      var expected01 = source01.ToJagged();
      var expected02 = source02.ToJagged();

      double[][]? actual01;
      double[][]? actual02;

      // act
      using (var server = new RVisServer())
      {
        var client = await server.OpenChannelAsync();

        await client.CreateMatrixAsync(source01.ToJagged(), nameof(source01));
        await client.CreateMatrixAsync(source02.ToJagged(), nameof(source02));

        var doubles = await client.EvaluateDoublesAsync(nameof(source01));
        actual01 = doubles.Select(ds => ds.Value.ToArray()).ToArray();

        doubles = await client.EvaluateDoublesAsync(nameof(source02));
        actual02 = doubles?
          .Select(ds => ds.Value.ToArray())
          .ToArray()
          .Transpose();
      }

      // assert
      Assert.AreEqual(expected01.GetType(), actual01.GetType());
      expected02.Iter((i,a) => Assert.IsTrue(a.SequenceEqual(actual02![i])));
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public async Task TestSaveLoadBinary()
    {
      // arrange
      const string expected = "Test String !£$%^&*()_+";
      string? actual;

      using (var server = new RVisServer())
      {
        var client = await server.OpenChannelAsync();

        var code = $"expected <- \"{expected}\"";
        await client.EvaluateNonQueryAsync(code);

        // act
        var binary = await client.SaveObjectToBinaryAsync("expected");
        await client.ClearAsync();
        await client.LoadFromBinaryAsync(binary);
        var strings = await client.EvaluateStringsAsync("expected");
        actual = strings.First().Value[0];
      }

      // assert
      Assert.AreEqual(expected, actual);
    }
#endif
  }
}
